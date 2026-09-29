using System;
using System.Collections.Generic;

namespace BE.Seguridad
{
    public class LoginRequestDto
    {
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime? Expiration { get; set; }
        public BE_Usuario? Usuario { get; set; }
        public List<BE_Menu>? Permisos { get; set; }
        public List<string>? UsuariosMantenerSesion { get; set; }
        public List<string>? UsuariosAnularEnviadoWMS { get; set; }
        public Dictionary<string, string>? PermisoModificarCondicionPago { get; set; }
    }

    public class TokenValidationDto
    {
        public bool IsValid { get; set; }
        public string? Usuario { get; set; }
        public string? Message { get; set; }
    }
}
