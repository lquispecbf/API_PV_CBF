using BE.Seguridad;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Permisos
{
    public interface IPermisosPvRepositorio
    {
        Task<PermisosPuntoVentaDTO> ObtenerPermisosUsuario(int idUsuario);
        Task<ConfiguracionUsuarioMatrizResponseDTO?> ObtenerMatrizConfiguracionUsuario(int idUsuario);
        Task<bool> GuardarConfiguracionUsuario(int idUsuario, int idRolPv, List<string> accionesPermitidas, string usuarioModificacion);
        Task<List<UsuarioPvDTO>> ListarUsuariosPermisos(string? busqueda = null);
        Task<List<RolPvDTO>> ListarRoles();

        // Mantenimiento de Roles
        Task<RolMatrizResponseDTO?> ObtenerMatrizRol(int idRolPv);
        Task<int> GuardarRol(RolGuardarRequestDTO request, string usuarioModificacion);
        Task<bool> GuardarMatrizRolAcciones(int idRolPv, List<string> accionesPermitidas, string usuarioModificacion);
        Task<bool> CambiarEstadoRol(int idRolPv, string estado, string usuarioModificacion);
        Task<List<PlantillaRolDTO>> ObtenerPlantillasRoles();
    }
}
