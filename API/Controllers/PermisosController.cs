using API.Infrastructure.Filters;
using BE.Seguridad;
using BL.Servicios;
using DA.Repositorio.Repositorio_Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PermisosController : ControllerBase
    {
        private readonly IPermisosPvService _permisosService;
        private readonly IErrores _errores;

        public PermisosController(
            IPermisosPvService permisosService,
            IErrores errores)
        {
            _permisosService = permisosService;
            _errores = errores;
        }

        private string ObtenerUsuarioActual()
        {
            return User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("usuario") ?? "SYSTEM";
        }

        [HttpGet("usuarios")]
        public async Task<IActionResult> ListarUsuarios([FromQuery] string? busqueda)
        {
            try
            {
                var lista = await _permisosService.ListarUsuariosAsync(busqueda);
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al listar usuarios: " + ex.Message });
            }
        }

        [HttpGet("roles")]
        public async Task<IActionResult> ListarRoles()
        {
            try
            {
                var lista = await _permisosService.ListarRolesAsync();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al listar roles: " + ex.Message });
            }
        }

        [HttpGet("matriz/{idUsuario}")]
        public async Task<IActionResult> ObtenerMatrizUsuario(int idUsuario)
        {
            try
            {
                if (idUsuario <= 0)
                {
                    return BadRequest(new { error = "ID de usuario inválido." });
                }

                var matriz = await _permisosService.ObtenerMatrizConfiguracionAsync(idUsuario);
                if (matriz == null)
                {
                    return NotFound(new { error = "Usuario no encontrado." });
                }

                return Ok(matriz);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al obtener matriz de permisos: " + ex.Message });
            }
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarConfiguracion([FromBody] GuardarConfiguracionUsuarioRequestDTO request)
        {
            try
            {
                if (request == null || request.IdUsuario <= 0 || request.IdRolPv <= 0)
                {
                    return BadRequest(new { error = "Datos de configuración incompletos o inválidos." });
                }

                var usuarioModifica = ObtenerUsuarioActual();
                bool ok = await _permisosService.GuardarConfiguracionUsuarioAsync(request, usuarioModifica);

                if (!ok)
                {
                    return StatusCode(500, new { error = "No se pudo guardar la configuración del usuario." });
                }

                return Ok(new { success = true, mensaje = "Configuración de permisos guardada exitosamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al guardar permisos: " + ex.Message });
            }
        }

        [HttpGet("mis-permisos")]
        public async Task<IActionResult> ObtenerMisPermisos()
        {
            try
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario");
                if (!int.TryParse(idClaim, out int idUsuario))
                {
                    return Unauthorized();
                }

                var permisos = await _permisosService.ObtenerPermisosUsuarioAsync(idUsuario, ObtenerUsuarioActual());
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al consultar permisos: " + ex.Message });
            }
        }

        #region Endpoints Mantenimiento de Roles

        [HttpGet("roles/matriz/{idRolPv}")]
        public async Task<IActionResult> ObtenerMatrizRol(int idRolPv)
        {
            try
            {
                if (idRolPv < 0)
                {
                    return BadRequest(new { error = "ID de rol inválido." });
                }

                var matriz = await _permisosService.ObtenerMatrizRolAsync(idRolPv);
                if (matriz == null)
                {
                    return NotFound(new { error = "Rol no encontrado." });
                }

                return Ok(matriz);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al obtener matriz del rol: " + ex.Message });
            }
        }

        [HttpPost("roles/guardar")]
        public async Task<IActionResult> GuardarRol([FromBody] RolGuardarRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.NombreRol))
                {
                    return BadRequest(new { error = "El nombre del rol es obligatorio." });
                }

                if (request.IdRolPv <= 0 && string.IsNullOrWhiteSpace(request.CodigoRol))
                {
                    return BadRequest(new { error = "El código de rol es obligatorio para roles nuevos." });
                }

                var usuarioModifica = ObtenerUsuarioActual();
                int idRol = await _permisosService.GuardarRolAsync(request, usuarioModifica);

                if (idRol <= 0)
                {
                    return StatusCode(500, new { error = "No se pudo guardar el rol." });
                }

                return Ok(new { success = true, idRolPv = idRol, mensaje = "Rol y plantilla de acciones guardados exitosamente." });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al guardar rol: " + ex.Message });
            }
        }

        [HttpPost("roles/cambiar-estado")]
        public async Task<IActionResult> CambiarEstadoRol([FromBody] CambiarEstadoRolRequestDTO request)
        {
            try
            {
                if (request == null || request.IdRolPv <= 0 || string.IsNullOrWhiteSpace(request.Estado))
                {
                    return BadRequest(new { error = "Parámetros inválidos." });
                }

                var usuarioModifica = ObtenerUsuarioActual();
                bool ok = await _permisosService.CambiarEstadoRolAsync(request.IdRolPv, request.Estado.Trim().ToUpperInvariant(), usuarioModifica);

                if (!ok)
                {
                    return StatusCode(500, new { error = "No se pudo cambiar el estado del rol." });
                }

                string msg = request.Estado.Trim().ToUpperInvariant() == "A" ? "Rol activado correctamente." : "Rol desactivado correctamente.";
                return Ok(new { success = true, mensaje = msg });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al cambiar estado del rol: " + ex.Message });
            }
        }

        [HttpGet("roles/plantillas")]
        public async Task<IActionResult> ObtenerPlantillasRoles()
        {
            try
            {
                var plantillas = await _permisosService.ObtenerPlantillasRolesAsync();
                return Ok(plantillas);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al obtener plantillas de roles: " + ex.Message });
            }
        }

        #endregion
    }
}
