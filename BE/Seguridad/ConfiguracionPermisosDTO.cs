using System.Collections.Generic;

namespace BE.Seguridad
{
    public class RolPvDTO
    {
        public int IdRolPv { get; set; }
        public string CodigoRol { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool EsSistema { get; set; }
        public string Estado { get; set; } = "A";
        public bool Activo => Estado == "A";
        public int CantidadAcciones { get; set; }
        public int TotalAcciones => CantidadAcciones;
        public int TotalAccionesSistema { get; set; }
        public int CantidadUsuarios { get; set; }
        public int TotalUsuarios => CantidadUsuarios;
    }

    public class UsuarioPvDTO
    {
        public int IdUsuario { get; set; }
        public string UsuarioLogin { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string PerfilIntranet { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int IdRolPv { get; set; }
        public string NombreRolPv { get; set; } = string.Empty;
        public string CodigoRolPv { get; set; } = string.Empty;
        public bool TieneAccesoIntranetPv { get; set; }
    }

    public class MatrizPermisoItemDTO
    {
        public int IdAccion { get; set; }
        public string CodigoModulo { get; set; } = string.Empty;
        public string CodigoAccion { get; set; } = string.Empty;
        public string NombreAccion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public bool PermitidoPorRol { get; set; }
        public bool? PermitidoOverride { get; set; }
        public bool PermitidoFinal { get; set; }
        public bool TieneOverride { get; set; }
    }

    public class ConfiguracionUsuarioMatrizResponseDTO
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string UsuarioLogin { get; set; } = string.Empty;
        public string PerfilIntranet { get; set; } = string.Empty;
        public int IdRolPvActual { get; set; }
        public string CodigoRolActual { get; set; } = string.Empty;
        public string NombreRolActual { get; set; } = string.Empty;
        public bool TieneAccesoIntranetPv { get; set; }
        public List<MatrizPermisoItemDTO> Acciones { get; set; } = new List<MatrizPermisoItemDTO>();
    }

    public class GuardarConfiguracionUsuarioRequestDTO
    {
        public int IdUsuario { get; set; }
        public int IdRolPv { get; set; }
        public List<string> AccionesPermitidas { get; set; } = new List<string>();
    }

    public class RolGuardarRequestDTO
    {
        public int IdRolPv { get; set; }
        public string CodigoRol { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public List<string> AccionesPermitidas { get; set; } = new List<string>();
        public List<string>? AccionesBase
        {
            get => AccionesPermitidas;
            set { if (value != null) AccionesPermitidas = value; }
        }
    }

    public class RolMatrizResponseDTO
    {
        public int IdRolPv { get; set; }
        public string CodigoRol { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool EsSistema { get; set; }
        public string Estado { get; set; } = "A";
        public bool Activo => Estado == "A";
        public int CantidadUsuarios { get; set; }
        public int TotalUsuarios => CantidadUsuarios;
        public List<MatrizPermisoItemDTO> Acciones { get; set; } = new List<MatrizPermisoItemDTO>();
    }

    public class CambiarEstadoRolRequestDTO
    {
        public int IdRolPv { get; set; }
        public string Estado { get; set; } = "A"; // "A" o "I"
        public bool? Activo
        {
            get => Estado == "A";
            set { if (value.HasValue) Estado = value.Value ? "A" : "I"; }
        }
    }

    public class PlantillaRolDTO
    {
        public int IdRolPv { get; set; }
        public string CodigoRol { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool EsSistema { get; set; }
        public List<string> Acciones { get; set; } = new List<string>();
    }
}
