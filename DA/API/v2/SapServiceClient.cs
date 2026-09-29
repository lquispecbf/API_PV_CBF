using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DA.Configuracion;
using DA.API;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace DA.API.v2
{
    /// <summary>
    /// Cliente moderno para SAP B1 Service Layer.
    /// Coexiste con <see cref="API_ServiceLayer"/> (legacy) sin modificarlo.
    ///
    /// Mejoras respecto al cliente legacy:
    /// - HttpClient inyectado vía IHttpClientFactory (no instanciado por request).
    /// - Timeout configurable desde appsettings.json (ServiceLayerTimeoutSeconds).
    /// - Resiliencia empresarial con Polly 8.x (Retry exponencial + Jitter + Circuit Breaker + Timeout por intento).
    /// - Implementa interfaz ISapServiceClient para facilitar pruebas y DI.
    /// - Soporte para OData $batch (actualizaciones masivas en 1 round-trip).
    /// - Contexto estructurado de auditoría (SapLogContext) para trazabilidad por usuario y documento.
    /// </summary>
    public class SapServiceClient : ISapServiceClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _serviceLayerBase;
        private readonly string _companyDB;
        private readonly string _userName;
        private readonly string _password;
        private readonly ServiceLayerResilienciaOptions _resilienciaOpts;

        /// <summary>Nombre del Named Client registrado en IHttpClientFactory.</summary>
        public const string HttpClientName = "SAP";

        private const int MaxLogBodyLength = 4000;

        // ── Sesión compartida (estática) ────────────────────────────────────────
        private static readonly object _lock = new();
        private static readonly SemaphoreSlim _loginLock = new(1, 1);
        private static readonly CookieContainer _cookieContainer = new();
        private static string? _sessionId;
        private static DateTime _lastLogin = DateTime.MinValue;

        /// <summary>
        /// CookieContainer estático para uso al registrar el HttpClientHandler en DI.
        /// </summary>
        public static CookieContainer SharedCookieContainer => _cookieContainer;

        public SapServiceClient(IHttpClientFactory factory, IOptions<ConfiguracionConexion> config)
        {
            _httpClientFactory = factory;
            _serviceLayerBase = config.Value.ServiceLayerBase.TrimEnd('/') + "/";
            _companyDB = config.Value.CompanyDB;
            _userName = config.Value.UserName;
            _password = config.Value.Password;
            _resilienciaOpts = config.Value.ServiceLayerResiliencia ?? new ServiceLayerResilienciaOptions();
        }

        // ════════════════════════════════════════════════════════════════════════
        // SendAsync — operación individual con pipeline Polly
        // ════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<(bool success, string response)> SendAsync(
            HttpMethod method,
            string endpoint,
            object? payload = null,
            CancellationToken cancellationToken = default,
            SapLogContext? logContext = null)
        {
            var ctx = logContext ?? new SapLogContext("SAP2");
            if (string.IsNullOrWhiteSpace(ctx.Modulo))
            {
                ctx.Modulo = "SAP2";
            }

            var pipeline = SapResiliencePipeline.Obtener(_resilienciaOpts);
            await FileLogger.Info("================= INICIO LLAMADA SAP (SapServiceClient+Polly) =================", ctx);

            try
            {
                var httpResponse = await pipeline.ExecuteAsync(
                    async ct =>
                    {
                        await ObtenerSesionActiva(ct);

                        var client = _httpClientFactory.CreateClient(HttpClientName);
                        using var request = new HttpRequestMessage(method, _serviceLayerBase + endpoint);

                        if (payload != null)
                        {
                            string jsonPayload = JsonSerializer.Serialize(payload);
                            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                            await FileLogger.Info($"📤 Payload: {SanitizarParaLog(jsonPayload)}", ctx);
                        }

                        request.Headers.TryAddWithoutValidation("If-Match", "*");
                        request.Headers.TryAddWithoutValidation("Cookie", $"B1SESSION={_sessionId}; CompanyDB={Uri.EscapeDataString(_companyDB)}");

                        await FileLogger.Info($"➡️ {method} {_serviceLayerBase}{endpoint}", ctx);
                        var startTime = DateTime.Now;

                        var response = await client.SendAsync(request, ct);

                        var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                        await FileLogger.Info($"📊 Status: {(int)response.StatusCode} {response.StatusCode} | ⏱ {elapsed:F0} ms", ctx);

                        // Si la sesión expiró (401), invalidar para que el retry de Polly obtenga nueva sesión
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            lock (_lock)
                            {
                                _sessionId = null;
                                _lastLogin = DateTime.MinValue;
                            }
                        }

                        // Si SAP devuelve error de transacción interna
                        if (!response.IsSuccessStatusCode)
                        {
                            string errBody = await response.Content.ReadAsStringAsync(ct);
                            if (errBody.Contains("Could not commit transaction", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new SapCommitException(errBody);
                            }
                        }

                        return response;
                    },
                    cancellationToken);

                string body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                await FileLogger.Info($"📥 Body: {PrepararBodyParaLog(body)}", ctx);

                if (httpResponse.IsSuccessStatusCode)
                {
                    await FileLogger.Info("✅ Operación exitosa", ctx);
                    await FileLogger.Info("================= FIN LLAMADA SAP (OK) =================", ctx);
                    return (true, body);
                }

                await FileLogger.Error($"❌ Error no recuperable: {httpResponse.StatusCode} → {body}", ctx);
                await FileLogger.Info("================= FIN LLAMADA SAP (ERROR) =================", ctx);
                return (false, body);
            }
            catch (BrokenCircuitException ex)
            {
                string msg = "SAP Service Layer temporalmente no disponible (Circuit Breaker ABIERTO). Intente en unos momentos.";
                await FileLogger.Error($"🔴 [Circuit Breaker ABIERTO] {msg} - {ex.Message}", ctx);
                return (false, msg);
            }
            catch (TimeoutRejectedException ex)
            {
                string msg = $"Timeout en SAP Service Layer tras {_resilienciaOpts.MaxReintentos} reintentos.";
                await FileLogger.Error($"⏱ {msg} - {ex.Message}", ctx);
                return (false, msg);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await FileLogger.Warning("🚫 Operación cancelada por el usuario.", ctx);
                return (false, "Operación cancelada por el usuario.");
            }
            catch (Exception ex)
            {
                await FileLogger.Error($"💥 Excepción inesperada: {ex.Message}", ctx);
                return (false, $"Error al comunicarse con SAP: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SendBatchAsync — múltiples operaciones en 1 HTTP request OData $batch
        // ════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<SapBatchResult> SendBatchAsync(
            IEnumerable<SapBatchRequest> requests,
            CancellationToken cancellationToken = default,
            SapLogContext? logContext = null)
        {
            var ctx = logContext ?? new SapLogContext("SAP2");
            if (string.IsNullOrWhiteSpace(ctx.Modulo))
            {
                ctx.Modulo = "SAP2";
            }

            var requestList = requests?.ToList() ?? [];

            if (requestList.Count == 0)
            {
                return new SapBatchResult
                {
                    AllSucceeded = true,
                    TotalRequests = 0,
                    SucceededCount = 0,
                    FailedCount = 0,
                    Results = []
                };
            }

            await FileLogger.Info($"================= INICIO BATCH SAP ({requestList.Count} operaciones) =================", ctx);
            await ObtenerSesionActiva(cancellationToken);

            string boundary = $"batch_{Guid.NewGuid():N}";
            var batchBody = new StringBuilder();

            for (int i = 0; i < requestList.Count; i++)
            {
                var req = requestList[i];
                batchBody.AppendLine($"--{boundary}");
                batchBody.AppendLine("Content-Type: application/http");
                batchBody.AppendLine("Content-Transfer-Encoding: binary");
                batchBody.AppendLine();
                batchBody.AppendLine($"{req.Method.Method} {_serviceLayerBase}{req.Endpoint} HTTP/1.1");
                batchBody.AppendLine("If-Match: *");

                if (req.Payload != null)
                {
                    string json = JsonSerializer.Serialize(req.Payload);
                    batchBody.AppendLine("Content-Type: application/json");
                    batchBody.AppendLine();
                    batchBody.AppendLine(json);
                }
                else
                {
                    batchBody.AppendLine();
                }
            }

            batchBody.AppendLine($"--{boundary}--");

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var batchRequest = new HttpRequestMessage(HttpMethod.Post, _serviceLayerBase + "$batch");
            batchRequest.Headers.TryAddWithoutValidation("Cookie", $"B1SESSION={_sessionId}; CompanyDB={Uri.EscapeDataString(_companyDB)}");
            batchRequest.Content = new StringContent(batchBody.ToString(), Encoding.UTF8, $"multipart/mixed; boundary={boundary}");

            await FileLogger.Info($"➡️ POST $batch con {requestList.Count} operaciones", ctx);

            using var batchResponse = await client.SendAsync(batchRequest, cancellationToken);
            string batchBody2 = await batchResponse.Content.ReadAsStringAsync(cancellationToken);

            await FileLogger.Info($"📊 Batch Status: {(int)batchResponse.StatusCode} {batchResponse.StatusCode}", ctx);

            var resultados = ParsearRespuestaBatch(batchBody2, requestList.Count);

            int exitosos = resultados.Count(r => r.Success);
            int fallidos = resultados.Count(r => !r.Success);

            await FileLogger.Info($"✅ Batch completado: {exitosos} exitosos, {fallidos} fallidos", ctx);
            await FileLogger.Info("================= FIN BATCH SAP =================", ctx);

            return new SapBatchResult
            {
                AllSucceeded = fallidos == 0,
                TotalRequests = requestList.Count,
                SucceededCount = exitosos,
                FailedCount = fallidos,
                Results = resultados
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // Gestión de sesión SAP
        // ════════════════════════════════════════════════════════════════════════

        private async Task ObtenerSesionActiva(CancellationToken ct)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_sessionId) &&
                    DateTime.Now.Subtract(_lastLogin).TotalMinutes < 25)
                    return;
            }

            await FileLogger.Warning("⚠️ No hay sesión válida. Iniciando login...", "LOGIN2");

            await _loginLock.WaitAsync(ct);
            try
            {
                // Doble verificación tras obtener el lock
                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(_sessionId) &&
                        DateTime.Now.Subtract(_lastLogin).TotalMinutes < 25)
                        return;
                }

                var loginBody = new
                {
                    CompanyDB = _companyDB,
                    UserName = _userName,
                    Password = _password
                };

                string json = JsonSerializer.Serialize(loginBody);
                await FileLogger.Info("📤 Enviando login a SAP Service Layer...", "LOGIN2");

                var client = _httpClientFactory.CreateClient(HttpClientName);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(_serviceLayerBase + "Login", content, ct);

                string respBody = await resp.Content.ReadAsStringAsync(ct);
                await FileLogger.Info($"📊 Login Status: {resp.StatusCode}", "LOGIN2");

                if (!resp.IsSuccessStatusCode)
                {
                    string detalle = ExtraerResumenError(respBody);
                    string msg = $"Error login SAP. HTTP {(int)resp.StatusCode} {resp.StatusCode}. Detalle: {detalle}";
                    await FileLogger.Error($"❌ {msg}", "LOGIN2");
                    throw new HttpRequestException(msg, null, resp.StatusCode);
                }

                // Obtener B1SESSION desde CookieContainer
                var cookies = _cookieContainer.GetCookies(new Uri(_serviceLayerBase));
                string? sessionCookie = cookies["B1SESSION"]?.Value;

                // Fallback: parsear Set-Cookie header manualmente
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    if (resp.Headers.TryGetValues("Set-Cookie", out var setCookies))
                    {
                        var joined = string.Join(";", setCookies);
                        var m = Regex.Match(joined, "B1SESSION=([^;]+)");
                        if (m.Success)
                            sessionCookie = m.Groups[1].Value;
                    }
                }

                if (string.IsNullOrEmpty(sessionCookie))
                {
                    await FileLogger.Error("❌ No se obtuvo cookie B1SESSION", "LOGIN2");
                    throw new Exception($"No se recibió B1SESSION del Service Layer. Body: {respBody}");
                }

                lock (_lock)
                {
                    _sessionId = sessionCookie;
                    _lastLogin = DateTime.Now;
                }

                await FileLogger.Info($"✅ Login exitoso. SessionID: {MascararValor(_sessionId)}", "LOGIN2");
            }
            finally
            {
                _loginLock.Release();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Métodos auxiliares
        // ════════════════════════════════════════════════════════════════════════

        private static List<SapBatchPartResult> ParsearRespuestaBatch(string batchBody, int totalParts)
        {
            var resultados = new List<SapBatchPartResult>();
            if (string.IsNullOrWhiteSpace(batchBody))
                return resultados;

            var statusMatches = Regex.Matches(batchBody, @"HTTP/1\.1\s+(\d{3})\s+\w[^\r\n]*", RegexOptions.Multiline);

            var partBodies = Regex.Split(batchBody, @"--batch[^\r\n]*\r?\n")
                                  .Skip(1)
                                  .ToList();

            for (int i = 0; i < totalParts; i++)
            {
                int statusCode = 0;
                string body = string.Empty;

                if (i < statusMatches.Count)
                {
                    int.TryParse(statusMatches[i].Groups[1].Value, out statusCode);
                }

                if (i < partBodies.Count)
                {
                    var partContent = partBodies[i];
                    var blankLineIdx = Regex.Match(partContent, @"\r?\n\r?\n");
                    if (blankLineIdx.Success)
                    {
                        body = partContent[(blankLineIdx.Index + blankLineIdx.Length)..].Trim();
                    }
                }

                resultados.Add(new SapBatchPartResult
                {
                    Index = i,
                    StatusCode = statusCode,
                    Success = statusCode is >= 200 and < 300,
                    Body = body
                });
            }

            return resultados;
        }

        private static bool EsStatusTransitorio(HttpStatusCode? code)
        {
            return code == HttpStatusCode.RequestTimeout ||
                   code == HttpStatusCode.BadGateway ||
                   code == HttpStatusCode.ServiceUnavailable ||
                   code == HttpStatusCode.GatewayTimeout ||
                   code == (HttpStatusCode)429;
        }

        private static int CalcularEsperaMs(int intento) => intento switch
        {
            <= 1 => 2000,
            2 => 5000,
            3 => 10000,
            _ => 15000
        };

        private static string PrepararBodyParaLog(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "(sin contenido)";
            string sanitizado = SanitizarParaLog(body);
            return sanitizado.Length <= MaxLogBodyLength
                ? sanitizado
                : sanitizado[..MaxLogBodyLength] + "... (truncado)";
        }

        private static string SanitizarParaLog(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Regex.Replace(
                text,
                "(\"(?:Password|SessionId|SessionID|B1SESSION)\"\\s*:\\s*\")([^\"]*)(\")",
                "$1***$3",
                RegexOptions.IgnoreCase);
        }

        private static string MascararValor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "(vacío)";
            return value.Length <= 8 ? "***" : value[..8] + "...";
        }

        private static string ExtraerResumenError(string? body)
        {
            var (code, msg) = SapResponseParser.ExtraerDetalleError(body);
            return code != 0 ? $"[{code}] {msg}" : msg;
        }
    }
}
