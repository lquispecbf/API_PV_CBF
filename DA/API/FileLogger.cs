using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DA.API
{
    public static class FileLogger
    {
        private static string _basePathDefault = @"D:\LogServiceLayerPagado";
        private static readonly ConcurrentDictionary<string, string> _rutasPorModulo = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        /// <summary>
        /// Configura la ruta base predeterminada para los servicios que no tengan ruta específica asignada.
        /// </summary>
        public static void ConfigurarRutaDefault(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                _basePathDefault = path.TrimEnd('\\', '/');
            }
        }

        /// <summary>
        /// Configura una ruta base específica para un módulo determinado (ej: "ARTICULOS_SAP", "SAP2").
        /// </summary>
        public static void ConfigurarRutaModulo(string modulo, string path)
        {
            if (!string.IsNullOrWhiteSpace(modulo) && !string.IsNullOrWhiteSpace(path))
            {
                _rutasPorModulo[modulo.Trim()] = path.TrimEnd('\\', '/');
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Métodos con SapLogContext (Estructurados)
        // ════════════════════════════════════════════════════════════════════════

        public static Task Info(string message, SapLogContext? context)
            => WriteLog("INFO", message, context?.Modulo ?? "GENERAL", context?.DocEntry, context?.CustomPath, context?.Usuario, context?.DocNum, context?.ItemCode);

        public static Task Warning(string message, SapLogContext? context)
            => WriteLog("WARN", message, context?.Modulo ?? "GENERAL", context?.DocEntry, context?.CustomPath, context?.Usuario, context?.DocNum, context?.ItemCode);

        public static Task Error(string message, SapLogContext? context)
            => WriteLog("ERROR", message, context?.Modulo ?? "GENERAL", context?.DocEntry, context?.CustomPath, context?.Usuario, context?.DocNum, context?.ItemCode);

        public static Task Exception(Exception ex, SapLogContext? context)
        {
            var fullMessage = $"{ex.Message} | STACK: {ex.StackTrace}";
            return WriteLog("EXCEPTION", fullMessage, context?.Modulo ?? "GENERAL", context?.DocEntry, context?.CustomPath, context?.Usuario, context?.DocNum, context?.ItemCode);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Métodos Legacy / Parámetros individuales (Retrocompatibilidad total)
        // ════════════════════════════════════════════════════════════════════════

        public static Task Info(string message, string module = "GENERAL", string? docEntry = null, string? customPath = null, string? usuario = null, string? docNum = null, string? itemCode = null)
            => WriteLog("INFO", message, module, docEntry, customPath, usuario, docNum, itemCode);

        public static Task Warning(string message, string module = "GENERAL", string? docEntry = null, string? customPath = null, string? usuario = null, string? docNum = null, string? itemCode = null)
            => WriteLog("WARN", message, module, docEntry, customPath, usuario, docNum, itemCode);

        public static Task Error(string message, string module = "GENERAL", string? docEntry = null, string? customPath = null, string? usuario = null, string? docNum = null, string? itemCode = null)
            => WriteLog("ERROR", message, module, docEntry, customPath, usuario, docNum, itemCode);

        public static Task Exception(Exception ex, string module = "GENERAL", string? docEntry = null, string? customPath = null, string? usuario = null, string? docNum = null, string? itemCode = null)
        {
            var fullMessage = $"{ex.Message} | STACK: {ex.StackTrace}";
            return WriteLog("EXCEPTION", fullMessage, module, docEntry, customPath, usuario, docNum, itemCode);
        }

        private static Task WriteLog(string level, string message, string? module, string? docEntry, string? customPath, string? usuario = null, string? docNum = null, string? itemCode = null)
        {
            try
            {
                module = string.IsNullOrWhiteSpace(module) ? "GENERAL" : module.Trim();

                // Determinar ruta: customPath > ruta registrada por módulo > ruta default
                string? basePath = customPath;
                if (string.IsNullOrWhiteSpace(basePath) && !string.IsNullOrWhiteSpace(module))
                {
                    _rutasPorModulo.TryGetValue(module, out basePath);
                }
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    basePath = _basePathDefault;
                }

                // 🔹 Ruta con organización por año y mes (PRO)
                string year = DateTime.Now.ToString("yyyy");
                string month = DateTime.Now.ToString("MM");

                string fullPath = Path.Combine(basePath, year, month);

                // 🔥 Verificar y crear carpeta si no existe
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                // 🔹 Archivo por día
                string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
                string filePath = Path.Combine(fullPath, fileName);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                string logLine = $"[{timestamp}] [{level}]";

                if (!string.IsNullOrEmpty(usuario))
                    logLine += $" [Usuario:{usuario}]";

                if (!string.IsNullOrEmpty(docEntry))
                    logLine += $" [DocEntry:{docEntry}]";

                if (!string.IsNullOrEmpty(docNum))
                    logLine += $" [DocNum:{docNum}]";

                if (!string.IsNullOrEmpty(itemCode))
                    logLine += $" [ItemCode:{itemCode}]";

                logLine += $" → {message}{Environment.NewLine}";

                // 🔒 Thread-safe
                lock (_lock)
                {
                    File.AppendAllText(filePath, logLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Resiliencia: nunca romper la app por logs
            }

            return Task.CompletedTask;
        }
    }
}
