using BE.Seguridad;
using BL.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequiereAccionPvAttribute : TypeFilterAttribute
    {
        public RequiereAccionPvAttribute(params string[] acciones)
            : base(typeof(RequiereAccionPvFilter))
        {
            Arguments = new object[] { acciones };
        }
    }

    public class RequiereAccionPvFilter : IAsyncActionFilter
    {
        private readonly IPermisosPvService _permisosService;
        private readonly string[] _acciones;

        public RequiereAccionPvFilter(
            IPermisosPvService permisosService,
            string[] acciones)
        {
            _permisosService = permisosService;
            _acciones = acciones ?? Array.Empty<string>();
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "No autenticado." });
                return;
            }

            var idUsuarioClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? user.FindFirstValue("id_usuario");

            if (!int.TryParse(idUsuarioClaim, out int idUsuario) || idUsuario <= 0)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Identificador de usuario inválido." });
                return;
            }

            // Si no se especificaron acciones, solo requiere autenticación
            if (_acciones.Length == 0)
            {
                await next();
                return;
            }

            // Consultar permisos vigentes del usuario en tiempo real (con caché en memoria e invalidación instantánea)
            var permisosUsuario = await _permisosService.ObtenerPermisosUsuarioAsync(idUsuario);

            bool autorizado = false;
            foreach (var accion in _acciones)
            {
                if (string.IsNullOrWhiteSpace(accion)) continue;
                if (permisosUsuario.TieneAccion(accion))
                {
                    autorizado = true;
                    break;
                }
            }

            if (!autorizado)
            {
                context.Result = new ObjectResult(new
                {
                    error = "No cuenta con permisos para realizar esta acción.",
                    accionesRequeridas = _acciones
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
