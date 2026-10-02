using BE.Seguridad;
using DA.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Permisos
{
    public class PermisosPvRepositorio : IPermisosPvRepositorio
    {
        private readonly ConfiguracionConexion _conexion;

        public PermisosPvRepositorio(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
        }

        public async Task<PermisosPuntoVentaDTO> ObtenerPermisosUsuario(int idUsuario)
        {
            var dto = new PermisosPuntoVentaDTO();

            try
            {
                using var conn = new SqlConnection(_conexion.CadenaSQL);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.SP_OBTENER_PERMISOS_PV_USUARIO", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", idUsuario);

                using var reader = await cmd.ExecuteReaderAsync();

                // Resultset 1: Rol
                if (await reader.ReadAsync())
                {
                    dto.IdRolPv = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
                    dto.CodigoRolPv = reader.IsDBNull(1) ? RolesPvConstantes.Vendedor : reader.GetString(1).Trim();
                    dto.NombreRolPv = reader.IsDBNull(2) ? "Vendedor Estándar" : reader.GetString(2).Trim();
                }

                // Resultset 2: Acciones concedidas
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var codigoAccion = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim().ToUpperInvariant();
                        if (!string.IsNullOrWhiteSpace(codigoAccion) && !dto.Acciones.Contains(codigoAccion))
                        {
                            dto.Acciones.Add(codigoAccion);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback seguro en caso de contingencia
                dto.CodigoRolPv = RolesPvConstantes.Vendedor;
                dto.NombreRolPv = "Vendedor Estándar (Fallback)";
                dto.Acciones = new List<string>
                {
                    AccionesPvConstantes.VentaVer,
                    AccionesPvConstantes.VentaCrear,
                    AccionesPvConstantes.VentaGuardarBorrador,
                    AccionesPvConstantes.VentaAnular,
                    AccionesPvConstantes.VentaEnviarWms,
                    AccionesPvConstantes.VentaReabrir,
                    AccionesPvConstantes.VentaImprimir,
                    AccionesPvConstantes.VentaExportarExcel,
                    AccionesPvConstantes.StockAlmacenVer,
                    AccionesPvConstantes.StockAlmacenExportar
                };
            }

            return dto;
        }

        public async Task<ConfiguracionUsuarioMatrizResponseDTO?> ObtenerMatrizConfiguracionUsuario(int idUsuario)
        {
            ConfiguracionUsuarioMatrizResponseDTO? response = null;

            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_OBTENER_MATRIZ_CONFIGURACION_PV_USUARIO", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID_USUARIO", idUsuario);

            using var reader = await cmd.ExecuteReaderAsync();

            // Resultset 1: Cabecera Usuario
            if (await reader.ReadAsync())
            {
                response = new ConfiguracionUsuarioMatrizResponseDTO
                {
                    IdUsuario = Convert.ToInt32(reader["ID_USUARIO"]),
                    NombreCompleto = reader["NOMBRE_COMPLETO"]?.ToString() ?? "",
                    UsuarioLogin = reader["USUARIO_LOGIN"]?.ToString() ?? "",
                    PerfilIntranet = reader["PERFIL_INTRANET"]?.ToString() ?? "",
                    IdRolPvActual = Convert.ToInt32(reader["ID_ROL_PV_ACTUAL"]),
                    CodigoRolActual = reader["CODIGO_ROL_ACTUAL"]?.ToString() ?? "",
                    NombreRolActual = reader["NOMBRE_ROL_ACTUAL"]?.ToString() ?? "",
                    Acciones = new List<MatrizPermisoItemDTO>()
                };
            }

            if (response == null) return null;

            // Resultset 2: Acciones
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    response.Acciones.Add(new MatrizPermisoItemDTO
                    {
                        IdAccion = Convert.ToInt32(reader["ID_ACCION"]),
                        CodigoModulo = reader["CODIGO_MODULO"]?.ToString() ?? "",
                        CodigoAccion = reader["CODIGO_ACCION"]?.ToString() ?? "",
                        NombreAccion = reader["NOMBRE_ACCION"]?.ToString() ?? "",
                        Descripcion = reader["DESCRIPCION"]?.ToString(),
                        Orden = Convert.ToInt32(reader["ORDEN"]),
                        PermitidoPorRol = Convert.ToBoolean(reader["PERMITIDO_POR_ROL"]),
                        PermitidoOverride = reader.IsDBNull(reader.GetOrdinal("PERMITIDO_OVERRIDE")) ? null : (bool?)Convert.ToBoolean(reader["PERMITIDO_OVERRIDE"]),
                        PermitidoFinal = Convert.ToBoolean(reader["PERMITIDO_FINAL"]),
                        TieneOverride = Convert.ToBoolean(reader["ES_OVERRIDE"])
                    });
                }
            }

            return response;
        }

        public async Task<bool> GuardarConfiguracionUsuario(int idUsuario, int idRolPv, List<string> accionesPermitidas, string usuarioModificacion)
        {
            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_GUARDAR_CONFIGURACION_PV_USUARIO", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID_USUARIO", idUsuario);
            cmd.Parameters.AddWithValue("@ID_ROL_PV", idRolPv);
            cmd.Parameters.AddWithValue("@ACCIONES_PERMITIDAS_CSV", string.Join(",", accionesPermitidas ?? new List<string>()));
            cmd.Parameters.AddWithValue("@USUARIO_MODIFICA", usuarioModificacion);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var resultado = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
                return resultado == 1;
            }

            return false;
        }

        public async Task<List<UsuarioPvDTO>> ListarUsuariosPermisos(string? busqueda = null)
        {
            var lista = new List<UsuarioPvDTO>();

            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_LISTAR_USUARIOS_PERMISOS_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CRITERIO_BUSQUEDA", string.IsNullOrWhiteSpace(busqueda) ? (object)DBNull.Value : busqueda.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new UsuarioPvDTO
                {
                    IdUsuario = Convert.ToInt32(reader["ID_USUARIO"]),
                    UsuarioLogin = reader["USUARIO_LOGIN"]?.ToString() ?? "",
                    NombreCompleto = reader["NOMBRE_COMPLETO"]?.ToString() ?? "",
                    PerfilIntranet = reader["PERFIL_INTRANET"]?.ToString() ?? "",
                    Correo = "",
                    IdRolPv = Convert.ToInt32(reader["ID_ROL_PV"]),
                    NombreRolPv = reader["NOMBRE_ROL_PV"]?.ToString() ?? "",
                    CodigoRolPv = reader["CODIGO_ROL_PV"]?.ToString() ?? "",
                    TieneAccesoIntranetPv = Convert.ToInt32(reader["TIENE_ACCESO_PUNTO_VENTA"]) == 1
                });
            }

            return lista;
        }

        public async Task<List<RolPvDTO>> ListarRoles()
        {
            var lista = new List<RolPvDTO>();

            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_LISTAR_ROLES_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new RolPvDTO
                {
                    IdRolPv = Convert.ToInt32(reader["ID_ROL_PV"]),
                    CodigoRol = reader["CODIGO_ROL"]?.ToString() ?? "",
                    NombreRol = reader["NOMBRE_ROL"]?.ToString() ?? "",
                    Descripcion = reader["DESCRIPCION"]?.ToString(),
                    EsSistema = Convert.ToBoolean(reader["ES_SISTEMA"]),
                    Estado = reader["ESTADO"]?.ToString() ?? "A",
                    CantidadAcciones = reader["CANTIDAD_ACCIONES"] != DBNull.Value ? Convert.ToInt32(reader["CANTIDAD_ACCIONES"]) : 0,
                    TotalAccionesSistema = reader["TOTAL_ACCIONES_SISTEMA"] != DBNull.Value ? Convert.ToInt32(reader["TOTAL_ACCIONES_SISTEMA"]) : 0,
                    CantidadUsuarios = reader["CANTIDAD_USUARIOS"] != DBNull.Value ? Convert.ToInt32(reader["CANTIDAD_USUARIOS"]) : 0
                });
            }

            return lista;
        }

        #region Mantenimiento de Roles

        public async Task<RolMatrizResponseDTO?> ObtenerMatrizRol(int idRolPv)
        {
            RolMatrizResponseDTO? response = null;

            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_OBTENER_MATRIZ_ROL_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID_ROL_PV", idRolPv);

            using var reader = await cmd.ExecuteReaderAsync();

            // Resultset 1: Cabecera del Rol
            if (await reader.ReadAsync())
            {
                response = new RolMatrizResponseDTO
                {
                    IdRolPv = Convert.ToInt32(reader["ID_ROL_PV"]),
                    CodigoRol = reader["CODIGO_ROL"]?.ToString() ?? "",
                    NombreRol = reader["NOMBRE_ROL"]?.ToString() ?? "",
                    Descripcion = reader["DESCRIPCION"]?.ToString(),
                    EsSistema = Convert.ToBoolean(reader["ES_SISTEMA"]),
                    Estado = reader["ESTADO"]?.ToString() ?? "A",
                    CantidadUsuarios = reader["CANTIDAD_USUARIOS"] != DBNull.Value ? Convert.ToInt32(reader["CANTIDAD_USUARIOS"]) : 0,
                    Acciones = new List<MatrizPermisoItemDTO>()
                };
            }
            else if (idRolPv <= 0)
            {
                // Para roles nuevos, creamos la estructura base para recibir el catálogo de acciones
                response = new RolMatrizResponseDTO
                {
                    IdRolPv = 0,
                    CodigoRol = "",
                    NombreRol = "",
                    Descripcion = "",
                    EsSistema = false,
                    Estado = "A",
                    CantidadUsuarios = 0,
                    Acciones = new List<MatrizPermisoItemDTO>()
                };
            }

            if (response == null) return null;

            // Resultset 2: Matriz de Acciones
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    bool permitido = reader["PERMITIDO"] != DBNull.Value && Convert.ToBoolean(reader["PERMITIDO"]);
                    response.Acciones.Add(new MatrizPermisoItemDTO
                    {
                        IdAccion = Convert.ToInt32(reader["ID_ACCION"]),
                        CodigoModulo = reader["CODIGO_MODULO"]?.ToString() ?? "",
                        CodigoAccion = reader["CODIGO_ACCION"]?.ToString() ?? "",
                        NombreAccion = reader["NOMBRE_ACCION"]?.ToString() ?? "",
                        Descripcion = reader["DESCRIPCION"]?.ToString(),
                        Orden = reader["ORDEN"] != DBNull.Value ? Convert.ToInt32(reader["ORDEN"]) : 0,
                        PermitidoPorRol = permitido,
                        PermitidoFinal = permitido
                    });
                }
            }

            return response;
        }

        public async Task<int> GuardarRol(RolGuardarRequestDTO request, string usuarioModificacion)
        {
            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_GUARDAR_ROL_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID_ROL_PV", request.IdRolPv);
            cmd.Parameters.AddWithValue("@CODIGO_ROL", request.CodigoRol?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@NOMBRE_ROL", request.NombreRol?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@DESCRIPCION", request.Descripcion?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@USUARIO_REGISTRO", usuarioModificacion);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Convert.ToInt32(reader["ID_ROL_PV"]);
            }

            return 0;
        }

        public async Task<bool> GuardarMatrizRolAcciones(int idRolPv, List<string> accionesPermitidas, string usuarioModificacion)
        {
            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_GUARDAR_MATRIZ_ROL_ACCION_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID_ROL_PV", idRolPv);
            cmd.Parameters.AddWithValue("@ACCIONES_CSV", string.Join(",", accionesPermitidas ?? new List<string>()));
            cmd.Parameters.AddWithValue("@USUARIO_MODIFICACION", usuarioModificacion);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var resultado = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
                return resultado == 1;
            }

            return false;
        }

        public async Task<bool> CambiarEstadoRol(int idRolPv, string estado, string usuarioModificacion)
        {
            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_CAMBIAR_ESTADO_ROL_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID_ROL_PV", idRolPv);
            cmd.Parameters.AddWithValue("@ESTADO", estado);
            cmd.Parameters.AddWithValue("@USUARIO_MODIFICACION", usuarioModificacion);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var resultado = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
                return resultado == 1;
            }

            return false;
        }

        public async Task<List<PlantillaRolDTO>> ObtenerPlantillasRoles()
        {
            var plantillasMap = new Dictionary<int, PlantillaRolDTO>();

            using var conn = new SqlConnection(_conexion.CadenaSQL);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.SP_OBTENER_PLANTILLAS_ROLES_PV", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int idRol = Convert.ToInt32(reader["ID_ROL_PV"]);
                if (!plantillasMap.TryGetValue(idRol, out var p))
                {
                    p = new PlantillaRolDTO
                    {
                        IdRolPv = idRol,
                        CodigoRol = reader["CODIGO_ROL"]?.ToString() ?? "",
                        NombreRol = reader["NOMBRE_ROL"]?.ToString() ?? "",
                        Descripcion = reader["DESCRIPCION"]?.ToString(),
                        EsSistema = Convert.ToBoolean(reader["ES_SISTEMA"]),
                        Acciones = new List<string>()
                    };
                    plantillasMap[idRol] = p;
                }

                var codigoAccion = reader["CODIGO_ACCION"]?.ToString();
                if (!string.IsNullOrWhiteSpace(codigoAccion))
                {
                    p.Acciones.Add(codigoAccion.Trim().ToUpperInvariant());
                }
            }

            return new List<PlantillaRolDTO>(plantillasMap.Values);
        }

        #endregion
    }
}
