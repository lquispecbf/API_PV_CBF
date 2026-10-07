using BE.PuntoVenta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BL.Servicios
{
    public class DigemidService : IDigemidService
    {
        public const string HttpClientName = "DigemidClient";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DigemidService> _logger;

        // Control de concurrencia y retardo controlado entre peticiones salientes
        private static readonly SemaphoreSlim _throttler = new(1, 1);
        private static DateTime _ultimoAccesoUtc = DateTime.MinValue;
        private const int IntervaloMinimoMs = 2000; // 2 segundos mínimos entre peticiones a DIGEMID

        public DigemidService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DigemidService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DigemidConsultaResponseDTO> ConsultarEstablecimientosPorRucAsync(string ruc)
        {
            var rucLimpio = (ruc ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rucLimpio) || rucLimpio.Length != 11)
            {
                return new DigemidConsultaResponseDTO
                {
                    Success = false,
                    Ruc = rucLimpio,
                    Error = "El número de RUC debe tener 11 dígitos numéricos."
                };
            }

            // Simulación de Fallas para pruebas desde appsettings.json (Opción 1)
            bool simularFalla = _configuration.GetValue<bool>("Digemid:SimularFalla", false);
            if (simularFalla)
            {
                string tipoFalla = (_configuration.GetValue<string>("Digemid:TipoFalla") ?? "BLOQUEO_1015").Trim().ToUpperInvariant();
                _logger.LogWarning("[SIMULACION-DIGEMID] Simulación de falla activa ({TipoFalla}) para RUC {Ruc}", tipoFalla, rucLimpio);

                switch (tipoFalla)
                {
                    case "TIMEOUT":
                        return new DigemidConsultaResponseDTO
                        {
                            Success = false,
                            Ruc = rucLimpio,
                            Error = "El portal de DIGEMID tardó demasiado en responder (Tiempo de espera agotado). Intente nuevamente."
                        };
                    case "HTTP_500":
                    case "ERROR_500":
                        return new DigemidConsultaResponseDTO
                        {
                            Success = false,
                            Ruc = rucLimpio,
                            Error = "El servidor de DIGEMID respondió con estado HTTP 500 (Internal Server Error)."
                        };
                    case "OFFLINE":
                    case "SIN_SERVICIO":
                        return new DigemidConsultaResponseDTO
                        {
                            Success = false,
                            Ruc = rucLimpio,
                            Error = "El portal de DIGEMID (MINSA) se encuentra temporalmente fuera de servicio."
                        };
                    case "BLOQUEO_1015":
                    default:
                        return new DigemidConsultaResponseDTO
                        {
                            Success = false,
                            Ruc = rucLimpio,
                            Error = "El portal de DIGEMID se encuentra temporalmente ocupado o limitando consultas por alto tráfico. Por favor, volver a consultar."
                        };
                }
            }

            await _throttler.WaitAsync();
            try
            {
                // Aplicar retardo controlado para evitar ráfagas simultáneas que activen Cloudflare Rate Limiting
                var ahora = DateTime.UtcNow;
                var tiempoTranscurridoMs = (ahora - _ultimoAccesoUtc).TotalMilliseconds;
                if (tiempoTranscurridoMs < IntervaloMinimoMs)
                {
                    int delayRestante = IntervaloMinimoMs - (int)tiempoTranscurridoMs;
                    _logger.LogInformation("Aplicando retardo controlado de {Delay}ms antes de consultar DIGEMID para RUC {Ruc}", delayRestante, rucLimpio);
                    await Task.Delay(delayRestante);
                }

                _ultimoAccesoUtc = DateTime.UtcNow;

                var client = _httpClientFactory.CreateClient(HttpClientName);
                var url = $"https://serviciosweb-digemid.minsa.gob.pe/Consultas/Establecimientos?accion=QRY_E&param1=2&param2={Uri.EscapeDataString(rucLimpio)}&param3=&param4=&param5=&param6=&param7=&param8=&param9=";

                using var response = await client.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();

                // Detección de bloqueo por Cloudflare (Rate Limit / Error 1015 / 429)
                if (response.StatusCode == (HttpStatusCode)429 || 
                    (html != null && (html.Contains("Error 1015") || html.Contains("rate limited") || html.Contains("temporarily from accessing"))))
                {
                    _logger.LogWarning("Portal DIGEMID retornó Cloudflare Rate Limiting (Error 1015 / 429) para RUC {Ruc}", rucLimpio);
                    return new DigemidConsultaResponseDTO
                    {
                        Success = false,
                        Ruc = rucLimpio,
                        Error = "El portal de DIGEMID se encuentra temporalmente ocupado o limitando consultas por alto tráfico. Por favor, volver a consultar."
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Portal DIGEMID respondió con HTTP {StatusCode} para RUC {Ruc}", response.StatusCode, rucLimpio);
                    return new DigemidConsultaResponseDTO
                    {
                        Success = false,
                        Ruc = rucLimpio,
                        Error = $"El servidor de DIGEMID respondió con estado HTTP {(int)response.StatusCode}."
                    };
                }

                return ParsearRespuestaDigemid(html ?? "", rucLimpio);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Tiempo de espera agotado al consultar DIGEMID para RUC {Ruc}", rucLimpio);
                return new DigemidConsultaResponseDTO
                {
                    Success = false,
                    Ruc = rucLimpio,
                    Error = "El portal de DIGEMID tardó demasiado en responder (Tiempo de espera agotado). Intente nuevamente."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al consultar portal de DIGEMID para RUC {Ruc}", rucLimpio);
                return new DigemidConsultaResponseDTO
                {
                    Success = false,
                    Ruc = rucLimpio,
                    Error = "No se pudo comunicar con el portal de DIGEMID. Por favor, intente nuevamente."
                };
            }
            finally
            {
                _throttler.Release();
            }
        }

        private static DigemidConsultaResponseDTO ParsearRespuestaDigemid(string html, string ruc)
        {
            var response = new DigemidConsultaResponseDTO
            {
                Success = true,
                Ruc = ruc,
                Establecimientos = new List<DigemidEstablecimientoDTO>()
            };

            if (string.IsNullOrWhiteSpace(html))
            {
                response.MensajeResumen = "No se recibió contenido de DIGEMID.";
                return response;
            }

            var matchTabla = Regex.Match(
                html,
                @"<table[^>]*id=[""']tresultados[""'][^>]*>(.*?)</table>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            if (!matchTabla.Success)
            {
                response.MensajeResumen = "No se encontraron establecimientos registrados en DIGEMID para este RUC.";
                return response;
            }

            var contenidoTabla = matchTabla.Groups[1].Value;
            var matchesTr = Regex.Matches(
                contenidoTabla,
                @"<tr[^>]*>(.*?)</tr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            foreach (Match tr in matchesTr)
            {
                var trHtml = tr.Groups[1].Value;
                var matchesTd = Regex.Matches(
                    trHtml,
                    @"<td[^>]*>(.*?)</td>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                if (matchesTd.Count < 9) continue;

                var celdas = new List<string>();
                foreach (Match td in matchesTd)
                {
                    var textoLimpio = Regex.Replace(td.Groups[1].Value, @"<[^>]+>", "").Trim();
                    textoLimpio = WebUtility.HtmlDecode(textoLimpio);
                    celdas.Add(textoLimpio);
                }

                if (celdas.Count >= 10 && int.TryParse(celdas[1], out _))
                {
                    var est = new DigemidEstablecimientoDTO
                    {
                        Item = celdas[1],
                        NumeroRegistro = celdas[2],
                        Categoria = celdas[3],
                        NombreComercial = celdas[4],
                        RazonSocial = celdas[5],
                        Ruc = celdas[6],
                        Direccion = celdas[7],
                        Ubigeo = celdas[8],
                        Situacion = celdas.Count > 9 ? celdas[9] : "",
                        Empadronado = celdas.Count > 10 ? celdas[10] : ""
                    };
                    response.Establecimientos.Add(est);
                }
            }

            response.TotalRegistros = response.Establecimientos.Count;
            response.TieneActivos = response.Establecimientos.Any(x => x.EsActivo);

            if (response.TotalRegistros == 0)
            {
                response.MensajeResumen = "No se encontraron establecimientos registrados para este RUC.";
            }
            else if (response.TieneActivos)
            {
                var activos = response.Establecimientos.Count(x => x.EsActivo);
                response.MensajeResumen = $"Se encontraron {response.TotalRegistros} establecimiento(s) ({activos} ACTIVO(S)).";
            }
            else
            {
                response.MensajeResumen = $"Se encontraron {response.TotalRegistros} establecimiento(s), pero NINGUNO se encuentra ACTIVO.";
            }

            return response;
        }
    }
}
