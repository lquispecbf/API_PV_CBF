using API.Infrastructure.Jwt;
using BE;
using BE.Seguridad;
using DA.Repositorio;
using DA.Repositorio.Repositorio_Errores;
using DA.Repositorio.Repositorio_Menu;
using DA.Repositorio.Repositorio_Usuario;
using DA.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogin _login;
        private readonly IErrores _errores;
        private readonly IUsuario _usuario;
        private readonly IMenu _menu;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly BL.Servicios.IPermisosPvService _permisosPvService;

        public AuthController(
            ILogin login,
            IErrores errores,
            IUsuario usuario,
            IMenu menu,
            IJwtService jwtService,
            IConfiguration configuration,
            BL.Servicios.IPermisosPvService permisosPvService)
        {
            _login = login;
            _errores = errores;
            _usuario = usuario;
            _menu = menu;
            _jwtService = jwtService;
            _configuration = configuration;
            _permisosPvService = permisosPvService;
        }

        private static bool ParsearFechaVencimiento(string? fechaTexto, out DateTime fecha)
        {
            fecha = default;
            if (string.IsNullOrWhiteSpace(fechaTexto)) return false;

            var culturePE = System.Globalization.CultureInfo.GetCultureInfo("es-PE");
            var cultureInv = System.Globalization.CultureInfo.InvariantCulture;

            var formatos = new[]
            {
                "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy hh:mm:ss tt", "dd/MM/yyyy", "d/M/yyyy HH:mm:ss", "d/M/yyyy",
                "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd",
                "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy hh:mm:ss tt", "MM/dd/yyyy", "M/d/yyyy"
            };

            if (DateTime.TryParse(fechaTexto, culturePE, System.Globalization.DateTimeStyles.None, out fecha)) return true;
            if (DateTime.TryParseExact(fechaTexto, formatos, culturePE, System.Globalization.DateTimeStyles.None, out fecha)) return true;
            if (DateTime.TryParseExact(fechaTexto, formatos, cultureInv, System.Globalization.DateTimeStyles.None, out fecha)) return true;
            if (DateTime.TryParse(fechaTexto, cultureInv, System.Globalization.DateTimeStyles.None, out fecha)) return true;
            return DateTime.TryParse(fechaTexto, out fecha);
        }

        private bool Debe_Cambiar_Clave(BE_Usuario usuario, int diasVigencia)
        {
            if (usuario.FORZAR_CAMBIO_CLAVE == "True" || usuario.FORZAR_CAMBIO_CLAVE == "1") return true;
            if (string.IsNullOrEmpty(usuario.FECHA_CAMBIO_CLAVE)) return true;

            if (ParsearFechaVencimiento(usuario.FECHA_CAMBIO_CLAVE, out DateTime fecha))
            {
                return DateTime.Now >= fecha;
            }
            return true;
        }

        private int Dias_Restantes_Clave(BE_Usuario usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.FECHA_CAMBIO_CLAVE)) return 999;
            if (!ParsearFechaVencimiento(usuario.FECHA_CAMBIO_CLAVE, out DateTime fecha)) return 999;
            return (int)Math.Ceiling((fecha.Date - DateTime.Today).TotalDays);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] BE_Usuario oUsuario)
        {
            try
            {
                var lista = await _login.Validar_Login(oUsuario);

                if (lista == null || lista.Count == 0)
                {
                    return Ok(new
                    {
                        Estado = false,
                        Mensaje = "Usuario o contraseña incorrecta.",
                        Usuario = new List<BE_Usuario>()
                    });
                }

                var usuario = lista[0];

                if (usuario.BLOQUEADO)
                {
                    return Ok(new
                    {
                        Estado = false,
                        Mensaje = "USUARIO BLOQUEADO. POR FAVOR COMUNÍQUESE CON T.I.",
                        Usuario = new List<BE_Usuario>()
                    });
                }

                var historial = await _usuario.Obtener_Historial_Claves(
                    new BE_CambioClave
                    {
                        ID = usuario.ID.ToString()
                    });

                bool esValido = false;

                if (historial == null || historial.Count == 0)
                {
                    if (usuario.CONTRASEÑA == oUsuario.CONTRASEÑA)
                        esValido = true;
                }
                else
                {
                    if (usuario.CONTRASEÑA == EncriptacionHelper.EncryptPlainTextToCipherText(oUsuario.CONTRASEÑA))
                        esValido = true;
                }

                if (!esValido)
                {
                    var estado = await _login.ControlIntentosLogin(usuario.ID, false);

                    if (estado.BLOQUEADO)
                    {
                        return Ok(new
                        {
                            Estado = false,
                            Mensaje = "USUARIO BLOQUEADO. POR FAVOR COMUNÍQUESE CON T.I.",
                            Usuario = new List<BE_Usuario>()
                        });
                    }

                    return Ok(new
                    {
                        Estado = false,
                        Mensaje = $"Contraseña incorrecta. Intento {estado.INTENTOS} de 5.",
                        Usuario = new List<BE_Usuario>()
                    });
                }

                await _login.ControlIntentosLogin(usuario.ID, true);

                int diasVigencia = _configuration.GetValue<int>("SeguridadClave:DiasVigencia", 15);
                bool debeCambiar = Debe_Cambiar_Clave(usuario, diasVigencia);
                int diasRestantes = Dias_Restantes_Clave(usuario);
                bool proximoVencer = !debeCambiar && diasRestantes <= 5;

                usuario.FORZAR_CAMBIO_CLAVE = debeCambiar ? "1" : "0";
                usuario.DIAS_RESTANTES_CLAVE = diasRestantes;
                usuario.PROXIMO_VENCER = proximoVencer ? "1" : "0";

                // Gatekeeper de Autenticación: Validar que el usuario cuente con al menos un menú activo en el ecosistema PVD
                var menusUsuario = await _menu.Mostrar_Menu(new BE_Usuario { ID = usuario.ID });

                var menusValidosPvd = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "menu_venta",
                    "menu_punto_venta_clientes_bloqueados",
                    "menu_punto_venta_articulos_fraccionados",
                    "menu_punto_venta_stock_por_almacen",
                    "menu_punto_venta_permisos"
                };

                var menusPvdUsuario = menusUsuario?
                    .Where(m => !string.IsNullOrWhiteSpace(m.DESCRIPCION_MENU_SYS) && menusValidosPvd.Contains(m.DESCRIPCION_MENU_SYS.Trim()))
                    .Select(m => m.DESCRIPCION_MENU_SYS.Trim().ToLowerInvariant())
                    .ToList() ?? new List<string>();

                if (menusPvdUsuario.Count == 0)
                {
                    return Ok(new
                    {
                        Estado = false,
                        Mensaje = "Acceso Denegado: Su usuario no cuenta con autorización para acceder al Sistema de Punto de Venta. Comuníquese con el Administrador o T.I.",
                        Usuario = new List<BE_Usuario>()
                    });
                }

                // Determinar ruta inicial recomendada según los menús autorizados del usuario
                string urlInicial = "/PuntoVenta/Venta";
                if (!menusPvdUsuario.Contains("menu_venta"))
                {
                    if (menusPvdUsuario.Contains("menu_punto_venta_permisos"))
                        urlInicial = "/Permisos/Index";
                    else if (menusPvdUsuario.Contains("menu_punto_venta_clientes_bloqueados"))
                        urlInicial = "/PuntoVenta/ClienteBloqueado";
                    else if (menusPvdUsuario.Contains("menu_punto_venta_articulos_fraccionados"))
                        urlInicial = "/PuntoVenta/ArticuloFraccionado";
                    else if (menusPvdUsuario.Contains("menu_punto_venta_stock_por_almacen"))
                        urlInicial = "/PuntoVenta/StockPorAlmacen";
                }

                // Obtener permisos y Rol PVD desacoplados
                var permisosPv = await _permisosPvService.ObtenerPermisosUsuarioAsync(usuario.ID, usuario.USUARIO);

                // Generar Token JWT con claims de Rol PVD y Acciones
                string token = _jwtService.GenerarToken(usuario, permisosPv, out DateTime expiration);

                return Ok(new
                {
                    Estado = true,
                    Mensaje = "Acceso concedido.",
                    Token = token,
                    Expiration = expiration,
                    Usuario = lista,
                    MenusPvd = menusPvdUsuario,
                    UrlInicial = urlInicial,
                    PermisosPv = permisosPv,
                    MantenerSesion = permisosPv.MantenerSesion,
                    PuedeAnularEnviadoWms = permisosPv.PuedeAnularEnviadoWms,
                    RolCondicionPago = permisosPv.PuedeModificarCondicionPago ? "SUPERVISOR" : null
                });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new
                {
                    Estado = false,
                    Mensaje = $"Ocurrió un error interno del servidor: {ex.Message}",
                    Usuario = new List<BE_Usuario>()
                });
            }
        }

        [HttpPost("cambiar-clave")]
        [Authorize]
        public async Task<IActionResult> Cambiar_Clave([FromBody] BE_CambioClave oUsuario)
        {
            try
            {
                var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario");
                if (string.IsNullOrWhiteSpace(idUsuarioClaim))
                {
                    return Unauthorized(new { resultado = 0, mensaje = "Token inválido o expirado." });
                }

                oUsuario.ID = idUsuarioClaim;

                if (string.IsNullOrWhiteSpace(oUsuario.CLAVE_ACTUAL) || string.IsNullOrWhiteSpace(oUsuario.CLAVE_NUEVA))
                {
                    return BadRequest(new { resultado = 0, mensaje = "Debe ingresar la clave actual y la nueva." });
                }

                if (!EncriptacionHelper.ValidarClave(oUsuario.CLAVE_NUEVA))
                {
                    return BadRequest(new { resultado = 0, mensaje = "La clave debe tener al menos 8 caracteres, 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial." });
                }

                string claveEnBD = await _usuario.Obtener_Clave_Usuario(Convert.ToInt32(oUsuario.ID));
                string claveActualEncriptada = EncriptacionHelper.EncryptPlainTextToCipherText(oUsuario.CLAVE_ACTUAL);

                if (claveEnBD != claveActualEncriptada && claveEnBD != oUsuario.CLAVE_ACTUAL)
                {
                    return BadRequest(new { resultado = 0, mensaje = "La clave actual es incorrecta." });
                }

                string claveNuevaEncriptada = EncriptacionHelper.EncryptPlainTextToCipherText(oUsuario.CLAVE_NUEVA);
                if (claveEnBD == claveNuevaEncriptada || claveEnBD == oUsuario.CLAVE_NUEVA)
                {
                    return BadRequest(new { resultado = 0, mensaje = "La nueva clave no puede ser igual a la actual." });
                }

                var historial = await _usuario.Obtener_Historial_Claves(oUsuario);
                if (historial != null && historial.Any(h => h.Trim() == claveNuevaEncriptada.Trim()))
                {
                    return BadRequest(new { resultado = 0, mensaje = "No puede usar una clave utilizada anteriormente." });
                }

                oUsuario.USUARIO_MODIFICACION = User.FindFirstValue(ClaimTypes.Name) ?? "SYSTEM";
                int diasVigencia = _configuration.GetValue<int>("SeguridadClave:DiasVigencia", 15);
                oUsuario.FECHA_CAMBIO_CLAVE = DateTime.Now.AddDays(diasVigencia).ToString("yyyy-MM-dd HH:mm:ss");

                int resultado = await _usuario.Cambiar_Clave_Segura(oUsuario);

                return Ok(new
                {
                    resultado,
                    mensaje = resultado > 0 ? "Contraseña actualizada satisfactoriamente." : "No se pudo cambiar la contraseña."
                });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { resultado = 0, mensaje = $"Ocurrió un error interno al cambiar la clave: {ex.Message}" });
            }
        }

        [HttpGet("keepalive")]
        [Authorize]
        public IActionResult KeepAlive()
        {
            var idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario");
            var usuario = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("usuario");

            return Ok(new
            {
                Success = true,
                Message = "Token y sesión activos",
                IdUsuario = idUsuario,
                Usuario = usuario,
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpPost("renovar-token-sesion")]
        [AllowAnonymous]
        public async Task<IActionResult> RenovarTokenSesion([FromBody] RenovarTokenSesionRequestDTO request)
        {
            try
            {
                if (request == null || request.IdUsuario <= 0 || string.IsNullOrWhiteSpace(request.Usuario))
                {
                    return BadRequest(new RenovarTokenSesionResponseDTO
                    {
                        Success = false,
                        Mensaje = "Datos de sesión insuficientes."
                    });
                }

                var lista = await _usuario.Obtener_Usuarios(new BE_Usuario { ID = request.IdUsuario });
                var usuario = lista?.FirstOrDefault();
                if (usuario == null || !string.Equals(usuario.USUARIO?.Trim(), request.Usuario.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(new RenovarTokenSesionResponseDTO
                    {
                        Success = false,
                        Mensaje = "Usuario no válido o inactivo."
                    });
                }

                var permisosPv = await _permisosPvService.ObtenerPermisosUsuarioAsync(request.IdUsuario, usuario.USUARIO);
                string token = _jwtService.GenerarToken(usuario, permisosPv, out DateTime expiration);

                return Ok(new RenovarTokenSesionResponseDTO
                {
                    Success = true,
                    Token = token,
                    Expiration = expiration,
                    Mensaje = "Token renovado exitosamente."
                });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new RenovarTokenSesionResponseDTO
                {
                    Success = false,
                    Mensaje = "Error al renovar token: " + ex.Message
                });
            }
        }

        [HttpPost("mostrar-menu")]
        [HttpPost("Mostrar_Menu")]
        [Authorize]
        public async Task<IActionResult> Mostrar_Menu([FromBody] BE_Usuario? obj)
        {
            try
            {
                int idUsuario = 0;
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario");
                if (!int.TryParse(idClaim, out idUsuario) && obj != null)
                {
                    idUsuario = obj.ID;
                }

                var lista = await _menu.Mostrar_Menu(new BE_Usuario { ID = idUsuario });
                return Ok(lista ?? new List<BE_Usuario>());
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return Ok(new List<BE_Usuario>());
            }
        }

        [HttpGet("validar-permiso")]
        [Authorize]
        public async Task<IActionResult> ValidarPermiso([FromQuery] string? controller, [FromQuery] string? action)
        {
            try
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario");
                if (!int.TryParse(idClaim, out int idUsuario))
                {
                    return Unauthorized(new { tienePermiso = false });
                }

                if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
                {
                    return BadRequest(new { tienePermiso = false, error = "Controller y Action son requeridos." });
                }

                bool tienePermiso = await _menu.Validar_Permiso_Usuario_Accion(idUsuario, controller, action);
                return Ok(new { tienePermiso });
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { tienePermiso = false, error = ex.Message });
            }
        }

        [HttpGet("mis-permisos-pv")]
        [Authorize]
        public async Task<IActionResult> MisPermisosPv()
        {
            try
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario");
                if (!int.TryParse(idClaim, out int idUsuario) || idUsuario <= 0)
                {
                    return Unauthorized(new { error = "No autenticado." });
                }

                var permisos = await _permisosPvService.ObtenerPermisosUsuarioAsync(idUsuario);
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                await _errores.Insertar_Exception(ex);
                return StatusCode(500, new { error = "Error al obtener permisos de usuario: " + ex.Message });
            }
        }

        [HttpGet("perfil")]
        [Authorize]
        public IActionResult Perfil()
        {
            return Ok(new
            {
                IdUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id_usuario"),
                Usuario = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("usuario"),
                Nombres = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue("nombres"),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Perfil = User.FindFirstValue("nombre_perfil") ?? User.FindFirstValue("id_perfil"),
                IdVendedor = User.FindFirstValue("id_vendedor"),
                IdSucursal = User.FindFirstValue("id_sucursal"),
                Sucursal = User.FindFirstValue("nombre_sucursal")
            });
        }
    }
}
