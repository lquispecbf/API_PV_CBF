using API.Infrastructure.Jwt;
using BE;
using BE.Seguridad;
using DA.Repositorio;
using DA.Repositorio.Repositorio_Errores;
using DA.Repositorio.Repositorio_Extranet;
using DA.Repositorio.Repositorio_Menu;
using DA.Repositorio.Repositorio_Usuario;
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

        public AuthController(
            ILogin login,
            IErrores errores,
            IUsuario usuario,
            IMenu menu,
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _login = login;
            _errores = errores;
            _usuario = usuario;
            _menu = menu;
            _jwtService = jwtService;
            _configuration = configuration;
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
                    if (usuario.CONTRASEÑA == CambioClave.EncryptPlainTextToCipherText(oUsuario.CONTRASEÑA))
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

                var usuariosMantener = _configuration.GetSection("PuntoVenta_UsuariosMantenerSesion").Get<List<string>>() ?? new List<string>();
                bool mantenerSesion = usuariosMantener.Any(u => u.Trim().Equals(usuario.USUARIO?.Trim(), StringComparison.OrdinalIgnoreCase));

                var usuariosAnularWms = _configuration.GetSection("PuntoVenta_UsuariosAnularEnviadoWMS").Get<List<string>>() ?? new List<string>();
                bool puedeAnularWms = usuariosAnularWms.Any(u => u.Trim().Equals(usuario.USUARIO?.Trim(), StringComparison.OrdinalIgnoreCase));

                var permisosCondicionPago = _configuration.GetSection("PuntoVenta_PermisoModificarCondicionPago").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
                string? rolCondicionPago = null;
                foreach (var kv in permisosCondicionPago)
                {
                    if (kv.Key.Trim().Equals(usuario.USUARIO?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        rolCondicionPago = kv.Value.Trim().ToUpperInvariant();
                        break;
                    }
                }

                usuario.FORZAR_CAMBIO_CLAVE = debeCambiar ? "1" : "0";
                usuario.DIAS_RESTANTES_CLAVE = diasRestantes;
                usuario.PROXIMO_VENCER = proximoVencer ? "1" : "0";

                // Obtener permisos de menú de Punto de Venta
                var permisos = await _menu.Listar_Permisos_PuntoVenta(new BE_Usuario { ID = usuario.ID });

                // Generar Token JWT
                string token = _jwtService.GenerarToken(usuario, out DateTime expiration);

                return Ok(new
                {
                    Estado = true,
                    Mensaje = "Acceso concedido.",
                    Token = token,
                    Expiration = expiration,
                    Usuario = lista,
                    Permisos = permisos,
                    MantenerSesion = mantenerSesion,
                    PuedeAnularEnviadoWms = puedeAnularWms,
                    RolCondicionPago = rolCondicionPago
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

                if (!CambioClave.ValidarClave(oUsuario.CLAVE_NUEVA))
                {
                    return BadRequest(new { resultado = 0, mensaje = "La clave debe tener al menos 8 caracteres, 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial." });
                }

                string claveEnBD = await _usuario.Obtener_Clave_Usuario(Convert.ToInt32(oUsuario.ID));
                string claveActualEncriptada = CambioClave.EncryptPlainTextToCipherText(oUsuario.CLAVE_ACTUAL);

                if (claveEnBD != claveActualEncriptada && claveEnBD != oUsuario.CLAVE_ACTUAL)
                {
                    return BadRequest(new { resultado = 0, mensaje = "La clave actual es incorrecta." });
                }

                string claveNuevaEncriptada = CambioClave.EncryptPlainTextToCipherText(oUsuario.CLAVE_NUEVA);
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
