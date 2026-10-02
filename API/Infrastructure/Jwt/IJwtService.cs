using BE;
using BE.Seguridad;
using System;
using System.Security.Claims;

namespace API.Infrastructure.Jwt
{
    public interface IJwtService
    {
        string GenerarToken(BE_Usuario usuario, out DateTime expiration);
        string GenerarToken(BE_Usuario usuario, PermisosPuntoVentaDTO? permisosPv, out DateTime expiration);
        ClaimsPrincipal? ValidarToken(string token);
    }
}
