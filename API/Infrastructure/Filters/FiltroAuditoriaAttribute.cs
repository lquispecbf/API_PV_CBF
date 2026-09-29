using API.Infrastructure.Auditoria;
using BE.Auditoria;
using DA.Repositorio.Repositorio_Auditoria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;

namespace API.Infrastructure.Filters;

public class FiltroAuditoriaAttribute : IAsyncActionFilter
{
    private readonly IAuditoria _auditoria;
    private readonly IEndpointAuditoria _endpointAuditoria;

    public FiltroAuditoriaAttribute(
        IAuditoria auditoria,
        IEndpointAuditoria endpointAuditoria)
    {
        _auditoria = auditoria;
        _endpointAuditoria = endpointAuditoria;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        string? usuario = httpContext.User.FindFirstValue(ClaimTypes.Name) 
                          ?? httpContext.User.FindFirstValue("usuario");

        string? usuarioId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? httpContext.User.FindFirstValue("id_usuario");

        string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
        string userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        string controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        string action = context.RouteData.Values["action"]?.ToString() ?? "";
        string metodoHttp = httpContext.Request.Method;
        string url = httpContext.Request.Path;
        string queryString = httpContext.Request.QueryString.ToString();

        string parametros = "";
        try
        {
            parametros = JsonSerializer.Serialize(context.ActionArguments);
            if (action.Equals("Login", StringComparison.OrdinalIgnoreCase) ||
                action.Equals("Validar_Login", StringComparison.OrdinalIgnoreCase) ||
                action.Equals("Cambiar_Clave", StringComparison.OrdinalIgnoreCase))
            {
                parametros = "{\"credenciales\":\"[OCULTADO]\"}";
            }
        }
        catch
        {
            parametros = "{}";
        }

        var (navegador, versionNavegador, so) = AnalizadorUserAgent.Analizar(userAgent);
        var inicio = DateTime.Now;

        ActionExecutedContext? executedContext = null;
        Exception? excepcion = null;

        try
        {
            executedContext = await next();
            excepcion = executedContext.Exception;
        }
        catch (Exception ex)
        {
            excepcion = ex;
            throw;
        }
        finally
        {
            try
            {
                var fin = DateTime.Now;
                int tiempoMs = (int)(fin - inicio).TotalMilliseconds;
                int codigoRespuesta = executedContext?.HttpContext.Response.StatusCode ?? (excepcion != null ? 500 : 200);

                string? usuarioFinal = httpContext.User.FindFirstValue(ClaimTypes.Name) 
                                       ?? httpContext.User.FindFirstValue("usuario") 
                                       ?? usuario;

                string? usuarioIdFinal = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                                         ?? httpContext.User.FindFirstValue("id_usuario") 
                                         ?? usuarioId;

                var endpoint = await _endpointAuditoria.ObtenerEndpoint(controller, action);
                if (endpoint == null && controller.Equals("Auth", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = await _endpointAuditoria.ObtenerEndpoint("Seguridad", action);
                }

                bool debeAuditar = endpoint?.AUDITAR ?? true; // Por defecto auditar en API
                string tipoAccion = endpoint?.TIPO_ACCION ?? (metodoHttp == "GET" ? "CONSULTA" : "MODIFICACION");

                if (debeAuditar)
                {
                    var auditoria = new BE_Auditoria
                    {
                        FECHA_HORA = DateTime.Now,
                        USUARIO = usuarioFinal ?? "ANONYMOUS",
                        USUARIO_ID = int.TryParse(usuarioIdFinal, out var uid) ? uid : null,
                        IP = ip,
                        USER_AGENT = userAgent,
                        SISTEMA_OPERATIVO = so,
                        DISPOSITIVO = "Web/API",
                        NAVEGADOR = $"{navegador} {versionNavegador}".Trim(),
                        CONTROLLER = controller,
                        ACTION = action,
                        URL = url,
                        QUERY_STRING = queryString,
                        METODO_HTTP = metodoHttp,
                        TIPO_ACCION = tipoAccion,
                        CODIGO_RESPUESTA = codigoRespuesta,
                        TIEMPO_MS = tiempoMs,
                        VISTA = null,
                        NOMBRE_ARCHIVO = null,
                        PARAMETROS = parametros,
                        EXCEPCION = excepcion?.ToString(),
                        TIENE_EXCEPCION = excepcion != null ? 1 : 0,
                        ORIGEN = "API_PUNTO_VENTA"
                    };

                    await _auditoria.RegistrarAuditoria(auditoria);
                }
            }
            catch
            {
                // Silencioso para no interrumpir el flujo
            }
        }
    }
}
