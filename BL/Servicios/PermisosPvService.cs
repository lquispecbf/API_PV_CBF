using BE.Seguridad;
using DA.Repositorio.Repositorio_Permisos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BL.Servicios
{
    public interface IPermisosPvService
    {
        Task<PermisosPuntoVentaDTO> ObtenerPermisosUsuarioAsync(int idUsuario, string? loginUsuario = null);
        Task<ConfiguracionUsuarioMatrizResponseDTO?> ObtenerMatrizConfiguracionAsync(int idUsuario);
        Task<bool> GuardarConfiguracionUsuarioAsync(GuardarConfiguracionUsuarioRequestDTO request, string usuarioModificacion);
        Task<List<UsuarioPvDTO>> ListarUsuariosAsync(string? busqueda = null);
        Task<List<RolPvDTO>> ListarRolesAsync();
        Task<bool> ValidarAccionUsuarioAsync(int idUsuario, string codigoAccion);

        // Mantenimiento de Roles
        Task<RolMatrizResponseDTO?> ObtenerMatrizRolAsync(int idRolPv);
        Task<int> GuardarRolAsync(RolGuardarRequestDTO request, string usuarioModificacion);
        Task<bool> GuardarAccionesRolAsync(int idRolPv, List<string> accionesPermitidas, string usuarioModificacion);
        Task<bool> CambiarEstadoRolAsync(int idRolPv, string estado, string usuarioModificacion);
        Task<List<PlantillaRolDTO>> ObtenerPlantillasRolesAsync();
    }

    public class PermisosPvService : IPermisosPvService
    {
        private readonly IPermisosPvRepositorio _repositorio;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermisosPvService> _logger;

        public PermisosPvService(
            IPermisosPvRepositorio repositorio,
            IMemoryCache cache,
            ILogger<PermisosPvService> logger)
        {
            _repositorio = repositorio;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PermisosPuntoVentaDTO> ObtenerPermisosUsuarioAsync(int idUsuario, string? loginUsuario = null)
        {
            string cacheKey = $"PV_PERMISOS_{idUsuario}";
            if (_cache.TryGetValue(cacheKey, out PermisosPuntoVentaDTO? cached) && cached != null)
            {
                return cached;
            }

            var permisos = await _repositorio.ObtenerPermisosUsuario(idUsuario);
            _cache.Set(cacheKey, permisos, TimeSpan.FromMinutes(5));
            return permisos;
        }

        public async Task<ConfiguracionUsuarioMatrizResponseDTO?> ObtenerMatrizConfiguracionAsync(int idUsuario)
        {
            return await _repositorio.ObtenerMatrizConfiguracionUsuario(idUsuario);
        }

        public async Task<bool> GuardarConfiguracionUsuarioAsync(GuardarConfiguracionUsuarioRequestDTO request, string usuarioModificacion)
        {
            if (request == null || request.IdUsuario <= 0) return false;
            var ok = await _repositorio.GuardarConfiguracionUsuario(
                request.IdUsuario,
                request.IdRolPv,
                request.AccionesPermitidas,
                usuarioModificacion
            );

            if (ok)
            {
                // Invalida inmediatamente la caché de permisos del usuario modificado
                _cache.Remove($"PV_PERMISOS_{request.IdUsuario}");
                _logger.LogInformation("Permisos PVD actualizados y caché invalidada para el usuario ID {IdUsuario}", request.IdUsuario);
            }

            return ok;
        }

        public async Task<List<UsuarioPvDTO>> ListarUsuariosAsync(string? busqueda = null)
        {
            return await _repositorio.ListarUsuariosPermisos(busqueda);
        }

        public async Task<List<RolPvDTO>> ListarRolesAsync()
        {
            return await _repositorio.ListarRoles();
        }

        public async Task<bool> ValidarAccionUsuarioAsync(int idUsuario, string codigoAccion)
        {
            if (idUsuario <= 0 || string.IsNullOrWhiteSpace(codigoAccion)) return false;
            var permisos = await ObtenerPermisosUsuarioAsync(idUsuario);
            return permisos.TieneAccion(codigoAccion);
        }

        #region Mantenimiento de Roles

        public async Task<RolMatrizResponseDTO?> ObtenerMatrizRolAsync(int idRolPv)
        {
            if (idRolPv < 0) return null;
            return await _repositorio.ObtenerMatrizRol(idRolPv);
        }

        public async Task<int> GuardarRolAsync(RolGuardarRequestDTO request, string usuarioModificacion)
        {
            if (request == null) return 0;
            int idRol = await _repositorio.GuardarRol(request, usuarioModificacion);
            if (idRol > 0 && request.AccionesPermitidas != null)
            {
                await _repositorio.GuardarMatrizRolAcciones(idRol, request.AccionesPermitidas, usuarioModificacion);
            }
            return idRol;
        }

        public async Task<bool> GuardarAccionesRolAsync(int idRolPv, List<string> accionesPermitidas, string usuarioModificacion)
        {
            if (idRolPv <= 0) return false;
            return await _repositorio.GuardarMatrizRolAcciones(idRolPv, accionesPermitidas, usuarioModificacion);
        }

        public async Task<bool> CambiarEstadoRolAsync(int idRolPv, string estado, string usuarioModificacion)
        {
            if (idRolPv <= 0) return false;
            return await _repositorio.CambiarEstadoRol(idRolPv, estado, usuarioModificacion);
        }

        public async Task<List<PlantillaRolDTO>> ObtenerPlantillasRolesAsync()
        {
            return await _repositorio.ObtenerPlantillasRoles();
        }

        #endregion
    }
}
