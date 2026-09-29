using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DA.Configuracion;
using System.Text.RegularExpressions;

namespace DA.API
{

 
    public class API_ServiceLayer
    {
        private readonly string _serviceLayerBase;
        private readonly string _companyDB;
        private readonly string _userName;
        private readonly string _password;
        private readonly bool _ignorarErroresCertificado;
        private const int MaxLoggedBodyLength = 4000;
        private const int MaxIntentosSap = 4;

        private static readonly object _lock = new();
        private static string? _sessionId;
        private static DateTime _lastLogin = DateTime.MinValue;

        // El semáforo protege SOLO la operación de login/refresh de sesión
        private static readonly SemaphoreSlim _sapLock = new SemaphoreSlim(1, 1);
        private static readonly CookieContainer _cookieContainer = new CookieContainer();
        private readonly HttpClient _client;

        public API_ServiceLayer(ConfiguracionConexion config)
        {
            _serviceLayerBase = config.ServiceLayerBase.TrimEnd('/') + "/";
            _companyDB = config.CompanyDB;
            _userName = config.UserName;
            _password = config.Password;
            _ignorarErroresCertificado = config.IgnorarErroresCertificadoServiceLayer;

            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = true
            };
            // TEMPORAL PARA DIAGNÓSTICO
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;
            //if (_ignorarErroresCertificado)
            //{
            //    handler.ServerCertificateCustomValidationCallback =
            //        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            //}

            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_serviceLayerBase),
                Timeout = TimeSpan.FromSeconds(120)
            };
        }


        public async Task<(bool success, string response)> SendAsync(HttpMethod method, string endpoint, object? payload = null)
        {
            int intentos = 0;
            string ultimoError = string.Empty;

            await FileLogger.Info($"================= INICIO LLAMADA SAP =================", "SAP");

            while (intentos < MaxIntentosSap)
            {
                intentos++;
                var startTime = DateTime.Now;

                try
                {
                    await FileLogger.Info($"🔄 Intento #{intentos} de {MaxIntentosSap}", "SAP");

                    // 🔹 SESIÓN
                    await FileLogger.Info("🔐 Verificando sesión activa...", "SAP");
                    await ObtenerSesionActiva();
                    await FileLogger.Info($"🔐 Sesión activa OK", "SAP");

                    using var request = new HttpRequestMessage(method, endpoint);

                    string jsonPayload = payload != null ? JsonSerializer.Serialize(payload) : "";

                    // 🔹 PAYLOAD
                    if (payload != null)
                    {
                        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        await FileLogger.Info($"📤 Payload enviado: {SanitizarJsonParaLog(jsonPayload)}", "SAP");
                    }
                    else
                    {
                        await FileLogger.Info($"📤 Sin payload (request vacío)", "SAP");
                    }

                    // 🔹 HEADERS
                    request.Headers.TryAddWithoutValidation("If-Match", "*");
                    await FileLogger.Info($"📑 Header agregado: If-Match = *", "SAP");

                    await FileLogger.Info($"➡️ Enviando request: {method} {endpoint}", "SAP");
                    await FileLogger.Info($"🌐 URL completa: {_client.BaseAddress}{endpoint}", "SAP");

                    // 🔹 ENVÍO
                    using var response = await _client.SendAsync(request);

                    var endTime = DateTime.Now;
                    var elapsed = (endTime - startTime).TotalMilliseconds;

                    string body = await response.Content.ReadAsStringAsync();

                    await FileLogger.Info($"⬅️ ================= RESPUESTA SAP =================", "SAP");

                    await FileLogger.Info($"📦 Content Length: {response.Content.Headers.ContentLength}", "SAP");
                    await FileLogger.Info($"🔤 Encoding: {response.Content.Headers.ContentType}", "SAP");

                    // 🔹 Código HTTP
                    await FileLogger.Info($"📊 Status Code: {(int)response.StatusCode} - {response.StatusCode}", "SAP");

                    // 🔹 Interpretación del código HTTP
                    string descripcionHttp = response.StatusCode switch
                    {
                        HttpStatusCode.OK => "OK - Operación exitosa",
                        HttpStatusCode.Created => "CREATED - Recurso creado correctamente",
                        HttpStatusCode.NoContent => "NO CONTENT - Sin contenido pero exitoso",
                        HttpStatusCode.BadRequest => "BAD REQUEST - Error en datos enviados",
                        HttpStatusCode.Unauthorized => "UNAUTHORIZED - Sesión inválida o expirada",
                        HttpStatusCode.Forbidden => "FORBIDDEN - Sin permisos",
                        HttpStatusCode.NotFound => "NOT FOUND - Recurso no existe",
                        HttpStatusCode.InternalServerError => "INTERNAL SERVER ERROR - Error interno en SAP",
                        _ => "Código no categorizado"
                    };

                    await FileLogger.Info($"🧠 Interpretación HTTP: {descripcionHttp}", "SAP");

                    // 🔹 Tiempo
                    await FileLogger.Info($"⏱ Tiempo respuesta: {elapsed} ms", "SAP");

                    // 🔹 Headers completos
                    await FileLogger.Info($"📑 Headers Response:", "SAP");
                    foreach (var header in response.Headers)
                    {
                        await FileLogger.Info($"   {header.Key}: {string.Join(",", header.Value)}", "SAP");
                    }

                    // 🔹 Headers de contenido
                    foreach (var header in response.Content.Headers)
                    {
                        await FileLogger.Info($"   {header.Key}: {string.Join(",", header.Value)}", "SAP");
                    }

                    await FileLogger.Info($"📥 Body Response (resumido): {PrepararBodyParaLog(body)}", "SAP");


                    if (TryObtenerErrorSap(body, out var sapErrorCode, out var sapErrorMessage))
                    {
                        await FileLogger.Error($"❌ SAP ERROR CODE: {sapErrorCode}", "SAP");
                        await FileLogger.Error($"❌ SAP ERROR MESSAGE: {sapErrorMessage}", "SAP");
                    }

                    await FileLogger.Info($"⬅️ ================================================", "SAP");

                    // 🔹 HEADERS RESPONSE
                    var headers = response.Headers.ToString();
                    await FileLogger.Info($"📑 Headers Response: {headers}", "SAP");

                    // 🔹 BODY RESPONSE
                    await FileLogger.Info($"📥 Body Response: {PrepararBodyParaLog(body)}", "SAP");

                    // 🔹 SUCCESS
                    if (response.IsSuccessStatusCode)
                    {
                        await FileLogger.Info($"✅ Operación exitosa en intento {intentos}", "SAP");
                        await FileLogger.Info($"================= FIN LLAMADA SAP (OK) =================", "SAP");

                        return (true, body);
                    }

                    if (EsStatusTransitorio(response.StatusCode))
                    {
                        ultimoError = $"HTTP {(int)response.StatusCode} {response.StatusCode}. Detalle: {ExtraerResumenErrorHttp(body)}";
                        int esperaMs = CalcularEsperaReintentoMs(intentos);

                        await FileLogger.Warning($"⚠️ Error transitorio SAP detectado: {ultimoError}", "SAP");
                        await FileLogger.Warning($"🔁 Reintentando en {esperaMs} ms...", "SAP");

                        await Task.Delay(esperaMs);
                        continue;
                    }

                    // 🔁 SESIÓN EXPIRADA
                    if (response.StatusCode == HttpStatusCode.Unauthorized ||
                        body.Contains("Session", StringComparison.OrdinalIgnoreCase) ||
                        body.Contains("Not logged in", StringComparison.OrdinalIgnoreCase))
                    {
                        await FileLogger.Warning("⚠️ Sesión expirada detectada", "SAP");

                        lock (_lock)
                        {
                            _sessionId = null;
                            _lastLogin = DateTime.MinValue;
                        }

                        await FileLogger.Warning("🔁 Se limpiará la sesión y se reintentará login...", "SAP");
                        continue;
                    }

                    // 🔁 ERROR INTERNO SAP
                    if (body.Contains("Could not commit transaction"))
                    {
                        await FileLogger.Warning("⚠️ Error interno SAP (-1116) detectado", "SAP");
                        await FileLogger.Warning($"⏳ Esperando 2 segundos antes de reintentar...", "SAP");

                        await Task.Delay(2000);
                        continue;
                    }

                    // ❌ ERROR FINAL
                    await FileLogger.Error($"❌ Error no recuperable detectado", "SAP");
                    await FileLogger.Error($"❌ Status: {response.StatusCode}", "SAP");
                    await FileLogger.Error($"❌ Body: {body}", "SAP");

                    await FileLogger.Info($"================= FIN LLAMADA SAP (ERROR) =================", "SAP");

                    return (false, body);
                }
                catch (TaskCanceledException)
                {
                    ultimoError = $"Timeout al llamar a SAP Service Layer. Endpoint: {method} {endpoint}";
                    await FileLogger.Warning($"⏱ Timeout en intento {intentos}", "SAP");
                    int esperaMs = CalcularEsperaReintentoMs(intentos);
                    await FileLogger.Warning($"🔁 Reintentando en {esperaMs} ms...", "SAP");

                    await Task.Delay(esperaMs);
                }
                catch (HttpRequestException ex) when (EsStatusTransitorio(ex.StatusCode))
                {
                    ultimoError = $"{ex.GetType().Name}: {ex.Message}";
                    int esperaMs = CalcularEsperaReintentoMs(intentos);

                    await FileLogger.Warning($"⚠️ Error transitorio de red/login SAP en intento {intentos}: {ultimoError}", "SAP");
                    await FileLogger.Warning($"🔁 Reintentando en {esperaMs} ms...", "SAP");
                    await FileLogger.Exception(ex, "SAP");

                    await Task.Delay(esperaMs);
                }
                catch (Exception ex)
                {
                    ultimoError = $"{ex.GetType().Name}: {ex.Message}";
                    await FileLogger.Error($"💥 Excepción en intento {intentos}", "SAP");
                    await FileLogger.Exception(ex, "SAP");

                    await Task.Delay(CalcularEsperaReintentoMs(intentos));
                }
            }

            await FileLogger.Error($"❌ Se agotaron todos los intentos", "SAP");
            await FileLogger.Info($"================= FIN LLAMADA SAP (FALLÓ TOTAL) =================", "SAP");

            string detalle = string.IsNullOrWhiteSpace(ultimoError)
                ? "Sin detalle adicional."
                : ultimoError;

            return (false, $"No se pudo completar la operación en SAP Service Layer tras múltiples intentos. Último error: {detalle}");
        }
        private async Task<string> ObtenerSesionActiva()
        {
            await FileLogger.Info("🔍 Validando sesión existente...", "LOGIN");

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_sessionId) &&
                    DateTime.Now.Subtract(_lastLogin).TotalMinutes < 25)
                {
                    return _sessionId;
                }
            }

            await FileLogger.Warning("⚠️ No hay sesión válida. Se iniciará login...", "LOGIN");

            await _sapLock.WaitAsync();
            try
            {
                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(_sessionId) &&
                        DateTime.Now.Subtract(_lastLogin).TotalMinutes < 25)
                    {
                        return _sessionId;
                    }
                }

                var loginBody = new
                {
                    CompanyDB = _companyDB,
                    UserName = _userName,
                    Password = _password
                };

                string json = JsonSerializer.Serialize(loginBody);

                await FileLogger.Info($"📤 Enviando login a SAP...", "LOGIN");
                await FileLogger.Info($"📤 Payload Login: {SanitizarJsonParaLog(json)}", "LOGIN");

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _client.PostAsync("Login", content);

                string respBody = await resp.Content.ReadAsStringAsync();

                await FileLogger.Info($"📥 Respuesta login recibida", "LOGIN");
                await FileLogger.Info($"📊 Status Login: {resp.StatusCode}", "LOGIN");
                await FileLogger.Info($"📥 Body Login: {PrepararBodyParaLog(respBody)}", "LOGIN");

                if (!resp.IsSuccessStatusCode)
                {
                    string detalle = ExtraerResumenErrorHttp(respBody);
                    string mensaje = $"Error login SAP. HTTP {(int)resp.StatusCode} {resp.StatusCode}. Detalle: {detalle}";

                    await FileLogger.Error($"❌ {mensaje}", "LOGIN");
                    throw new HttpRequestException(mensaje, null, resp.StatusCode);
                }

                var cookies = _cookieContainer.GetCookies(new Uri(_serviceLayerBase));
                var sessionCookie = cookies["B1SESSION"]?.Value;

                if (string.IsNullOrEmpty(sessionCookie))
                {
                    await FileLogger.Error("❌ No se obtuvo cookie B1SESSION", "LOGIN");
                    throw new Exception("No se obtuvo sesión SAP");
                }

                lock (_lock)
                {
                    _sessionId = sessionCookie;
                    _lastLogin = DateTime.Now;
                }

                await FileLogger.Info($"✅ Login exitoso", "LOGIN");
                await FileLogger.Info($"🔐 SessionID parcial: {MascararValor(_sessionId)}", "LOGIN");

                return _sessionId;
            }
            finally
            {
                _sapLock.Release();
            }
        }

        private static string PrepararBodyParaLog(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "(sin contenido)";

            var sanitized = SanitizarJsonParaLog(body);

            if (sanitized.Length <= MaxLoggedBodyLength)
                return sanitized;

            return sanitized[..MaxLoggedBodyLength] + "... (truncado)";
        }

        private static string SanitizarJsonParaLog(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var sanitized = Regex.Replace(
                text,
                "(\"(?:Password|SessionId|SessionID|B1SESSION)\"\\s*:\\s*\")([^\"]*)(\")",
                "$1***$3",
                RegexOptions.IgnoreCase);

            return sanitized;
        }

        private static string MascararValor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(vacío)";

            return value.Length <= 8 ? "***" : value[..8] + "...";
        }

        private static bool EsStatusTransitorio(HttpStatusCode? statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == HttpStatusCode.BadGateway ||
                   statusCode == HttpStatusCode.ServiceUnavailable ||
                   statusCode == HttpStatusCode.GatewayTimeout ||
                   statusCode == (HttpStatusCode)429;
        }

        private static int CalcularEsperaReintentoMs(int intento)
        {
            return intento switch
            {
                <= 1 => 2000,
                2 => 5000,
                3 => 10000,
                _ => 15000
            };
        }

        private static string ExtraerResumenErrorHttp(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "El servidor no devolvió detalle.";

            string limpio = Regex.Replace(body, "<.*?>", " ");
            limpio = WebUtility.HtmlDecode(limpio);
            limpio = Regex.Replace(limpio, "\\s+", " ").Trim();

            return limpio.Length <= 500 ? limpio : limpio[..500] + "...";
        }

        private static bool TryObtenerErrorSap(string? body, out string code, out string message)
        {
            code = string.Empty;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith("{"))
                return false;

            try
            {
                using var jsonDoc = JsonDocument.Parse(body);

                if (!jsonDoc.RootElement.TryGetProperty("error", out var error))
                    return false;

                if (error.TryGetProperty("code", out var codeElement))
                    code = codeElement.GetString() ?? string.Empty;

                if (error.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.ValueKind == JsonValueKind.Object &&
                        messageElement.TryGetProperty("value", out var valueElement))
                    {
                        message = valueElement.GetString() ?? string.Empty;
                    }
                    else if (messageElement.ValueKind == JsonValueKind.String)
                    {
                        message = messageElement.GetString() ?? string.Empty;
                    }
                }

                return !string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(message);
            }
            catch
            {
                return false;
            }
        }
        /*
        /// <summary>
        /// Envía una solicitud HTTP al SAP Service Layer con manejo de sesión y reintentos.
        /// </summary>
        public async Task<(bool success, string response)> SendAsync(HttpMethod method, string endpoint, object payload = null)
        {
            int intentos = 0;
            const int maxIntentos = 3;

            while (intentos < maxIntentos)
            {
                intentos++;

                try
                {
                    // Obtener o renovar sesión si es necesario (el propio método se encarga de la sincronización)
                    string sessionId = await ObtenerSesionActiva();

                    using var request = new HttpRequestMessage(method, endpoint);

                    if (payload != null)
                    {
                        request.Content = new StringContent(
                            JsonSerializer.Serialize(payload),
                            Encoding.UTF8,
                            "application/json"
                        );
                    }

                    // Cabeceras por petición (no en DefaultRequestHeaders)
                    request.Headers.TryAddWithoutValidation("If-Match", "*");

                    await FileLogger.Info($"➡️ [{DateTime.Now:HH:mm:ss}] Enviando {method} a {endpoint} (intento {intentos})...");

                    using var response = await _client.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        await FileLogger.Info($"✅ [{DateTime.Now:HH:mm:ss}] Operación exitosa en intento {intentos}.");
                        return (true, body);
                    }

                    // ⚠️ Manejo de errores comunes
                    if (response.StatusCode == HttpStatusCode.Unauthorized ||
                        body.Contains("Session", StringComparison.OrdinalIgnoreCase) ||
                        body.Contains("Not logged in", StringComparison.OrdinalIgnoreCase))
                    {
                        lock (_lock)
                        {
                            _sessionId = null!;
                            _lastLogin = DateTime.MinValue;
                        }

                        await FileLogger.Info($"⚠️ Sesión expirada. Reintentando login (intento {intentos})...");
                        continue;
                    }

                    if (body.Contains("Could not commit transaction"))
                    {
                          await FileLogger.Info($"⚠️ Error interno (-1116). Esperando 2 segundos antes del intento {intentos + 1}...");
                        await Task.Delay(2000);
                        continue;
                    }

                    // 🚫 Error no recuperable
                      await FileLogger.Info($"❌ Error SAP: {response.StatusCode} -> {body}");
                    return (false, body);
                }
                catch (TaskCanceledException)
                {
                      await FileLogger.Info($"⏱ Timeout SAP (intento {intentos}). Reintentando...");
                    await Task.Delay(1000 * intentos);
                }
                catch (Exception ex)
                {
                      await FileLogger.Info($"❌ Excepción intento {intentos}: {ex.Message}");
                    await Task.Delay(1500);
                }
            }

            return (false, "No se pudo completar la operación tras múltiples intentos. Ver logs.");
        }

        /// <summary>
        /// Devuelve una sesión SAP activa o realiza login si expiró.
        /// </summary>
        private async Task<string> ObtenerSesionActiva()
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_sessionId) &&
                    DateTime.Now.Subtract(_lastLogin).TotalMinutes < 25)
                {
                    return _sessionId;
                }
            }

              await FileLogger.Info($"🔐 [{DateTime.Now:HH:mm:ss}] Iniciando nueva sesión SAP Service Layer...");

            await _sapLock.WaitAsync();
            try
            {
                // Doble comprobación después de adquirir el semáforo
                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(_sessionId) &&
                        DateTime.Now.Subtract(_lastLogin).TotalMinutes < 25)
                    {
                        return _sessionId;
                    }
                }

                var loginBody = new
                {
                    CompanyDB = _companyDB,
                    UserName = _userName,
                    Password = _password
                };

                using var content = new StringContent(JsonSerializer.Serialize(loginBody), Encoding.UTF8, "application/json");
                using var resp = await _client.PostAsync("Login", content);

                string respBody = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al iniciar sesión en SAP: {resp.StatusCode} -> {respBody}");
                }

                // Intentar obtener cookie desde CookieContainer
                var cookies = _cookieContainer.GetCookies(new Uri(_serviceLayerBase));
                var sessionCookie = cookies["B1SESSION"]?.Value;

                // Fallback: revisar cabeceras Set-Cookie
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
                    throw new Exception($"No se recibió cookie B1SESSION del Service Layer. Body: {respBody}");

                lock (_lock)
                {
                    _sessionId = sessionCookie;
                    _lastLogin = DateTime.Now;
                }

                  await FileLogger.Info($"✅ [{DateTime.Now:HH:mm:ss}] Nueva sesión SAP iniciada: {_sessionId[..8]}...");
                return _sessionId;
            }
            finally
            {
                _sapLock.Release();
            }
        }
    */



    }
    
}
