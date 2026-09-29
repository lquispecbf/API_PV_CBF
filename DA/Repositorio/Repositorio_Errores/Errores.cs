using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Errores
{
    public class Errores : IErrores
    {
        public async Task Insertar_Exception(Exception ex)
        {
            string resultado_error = "";

            var task = new Task(() =>
            {
                try
                {
                    StackTrace st = new StackTrace(ex, true);
                    StackFrame? frame = st.GetFrames()?.FirstOrDefault(f => !string.IsNullOrEmpty(f.GetFileName())
                         && f.GetILOffset() != StackFrame.OFFSET_UNKNOWN
                         && f.GetNativeOffset() != StackFrame.OFFSET_UNKNOWN
                         && !f.GetMethod()?.Module.Assembly.GetName().Name.Contains("mscorlib") == true);

                    string MachineName = Environment.MachineName;
                    string UserName = Environment.UserName.ToUpper();
                    string Mensaje = ex.Message;
                    int LineaError = frame?.GetFileLineNumber() ?? 0;
                    string Proyecto = frame?.GetMethod()?.Module.Assembly.GetName().Name ?? "PuntoVenta";
                    string Clase = frame?.GetMethod()?.DeclaringType?.Name ?? "";
                    string metodo = frame?.GetMethod()?.Name ?? "";
                    string codigoError = frame != null ? Convert.ToString(frame.GetHashCode()) : "0";

                    resultado_error = "Equipo: " + MachineName + " | " + "Usuario: " + UserName + " | " + "Mensaje: " + Mensaje + " | " +
                    "LineaError: " + LineaError + " | " + "Proyecto: " + Proyecto + " | " + "Clase: " + Clase + " | " + "metodo: " + metodo +
                    " | " + "codigoError: " + codigoError;
                }
                catch
                {
                    resultado_error = $"Equipo: {Environment.MachineName} | Usuario: {Environment.UserName.ToUpper()} | Mensaje: {ex.Message} | StackTrace: {ex.StackTrace}";
                }
            });
            task.Start();
            await task;
            Log.Information(resultado_error);

        }
    }
}
