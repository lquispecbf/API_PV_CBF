using DA.Repositorio.Repositorio_Menu;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace API.Infrastructure.Filters
{
    public class RequierePermisoModuloAttribute : TypeFilterAttribute
    {
        public RequierePermisoModuloAttribute(params string[] permisos)
            : base(typeof(RequierePermisoModuloFilter))
        {
            Arguments = new object[] { permisos };
        }
    }

    public class RequierePermisoModuloFilter : IAsyncActionFilter
    {
        private readonly IMenu _menu;
        private readonly string[] _permisos;

        public RequierePermisoModuloFilter(
            IMenu menu,
            string[] permisos)
        {
            _menu = menu;
            _permisos = permisos ?? Array.Empty<string>();
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var idUsuarioClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                                 ?? context.HttpContext.User.FindFirstValue("id_usuario");

            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (_permisos.Length == 0)
            {
                await next();
                return;
            }

            bool tieneAlgunPermiso = false;

            foreach (var permiso in _permisos)
            {
                if (string.IsNullOrWhiteSpace(permiso))
                    continue;

                var partes = permiso.Split(':', StringSplitOptions.TrimEntries);

                if (partes.Length != 2 || string.IsNullOrWhiteSpace(partes[0]) || string.IsNullOrWhiteSpace(partes[1]))
                {
                    continue;
                }

                string controllerModulo = partes[0];
                string actionModulo = partes[1];

                bool tienePermiso = await _menu.Validar_Permiso_Usuario_Accion(
                    idUsuario,
                    controllerModulo,
                    actionModulo
                );

                if (tienePermiso)
                {
                    tieneAlgunPermiso = true;
                    break;
                }
            }

            if (!tieneAlgunPermiso)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            await next();
        }
    }
}
