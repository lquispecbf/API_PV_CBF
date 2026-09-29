using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DA.API.v2
{
    /// <summary>
    /// Utilitario para parsear y extraer mensajes estructurados de éxito o error 
    /// retornados por SAP Business One Service Layer (formato OData).
    /// </summary>
    public static class SapResponseParser
    {
        /// <summary>
        /// Extrae el código numérico de error y el mensaje legible de una respuesta JSON de SAP Service Layer.
        /// </summary>
        /// <param name="jsonResponse">Cadena JSON retornada por el Service Layer.</param>
        /// <returns>Tupla (int code, string message) con los detalles del error.</returns>
        public static (int code, string message) ExtraerDetalleError(string? jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                return (0, "Respuesta vacía del servidor SAP.");
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    int code = 0;
                    if (errorEl.TryGetProperty("code", out var codeEl) && codeEl.TryGetInt32(out int c))
                    {
                        code = c;
                    }

                    string mensaje = "Error no especificado por SAP Service Layer.";

                    if (errorEl.TryGetProperty("message", out var msgEl))
                    {
                        if (msgEl.ValueKind == JsonValueKind.Object && msgEl.TryGetProperty("value", out var valEl))
                        {
                            mensaje = valEl.GetString() ?? mensaje;
                        }
                        else if (msgEl.ValueKind == JsonValueKind.String)
                        {
                            mensaje = msgEl.GetString() ?? mensaje;
                        }
                    }

                    return (code, mensaje);
                }
            }
            catch
            {
                // Si la respuesta no es un JSON válido (ej: página HTML de error 502/504 del Web Server)
                string limpio = Regex.Replace(jsonResponse, "<.*?>", " ");
                limpio = System.Net.WebUtility.HtmlDecode(limpio);
                limpio = Regex.Replace(limpio, "\\s+", " ").Trim();
                return (0, limpio.Length > 300 ? limpio[..300] + "..." : limpio);
            }

            return (0, jsonResponse.Length > 300 ? jsonResponse[..300] + "..." : jsonResponse);
        }

        /// <summary>
        /// Formatea un mensaje amigable para mostrar al usuario final o registrar en logs.
        /// </summary>
        public static string FormatearMensajeError(int code, string message)
        {
            return code != 0 
                ? $"SAP Service Layer (Código {code}): {message}" 
                : $"SAP Service Layer: {message}";
        }
    }
}
