using BE;
using DA.Configuracion;
using DA.Seguridad;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Usuario
{
    public class Usuario : IUsuario
    {
        private readonly ConfiguracionConexion _conexion;
        public Usuario(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
        }

        public async Task<int> Grabar_Usuarios(BE_Usuario obj_Usuario)
        {
            int val = 0;

            // Aquí la conexión siempre se cerrará al finalizar este bloque, incluso si ocurre una excepción.
            using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
            {
                await con.OpenAsync(); // Mejor usar OpenAsync para no bloquear el hilo.

                // Lo mismo para la transacción
                using (SqlTransaction transaccion = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = con.CreateCommand())
                        {
                            cmd.Transaction = transaccion;
                            cmd.CommandText = "SP_INSERTAR_USUARIO";
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@APELLIDOS", obj_Usuario.APELLIDOS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NOMBRES", obj_Usuario.NOMBRES ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CORREO", obj_Usuario.CORREO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@USUARIO", obj_Usuario.USUARIO ?? (object)DBNull.Value);
                            //cmd.Parameters.AddWithValue("@CONTRASEÑA", obj_Usuario.CONTRASEÑA ?? (object)DBNull.Value);

                            //cmd.Parameters.AddWithValue("@CONTRASEÑA",string.IsNullOrWhiteSpace(obj_Usuario.CONTRASEÑA)? (object)DBNull.Value
                            //: CambioClave.EncryptPlainTextToCipherText(obj_Usuario.CONTRASEÑA));

                            cmd.Parameters.AddWithValue("@CONTRASEÑA", obj_Usuario.CONTRASEÑA ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@USUARIO_CREACION", "SYSTEM");

                            cmd.Parameters.AddWithValue("@TELEFONO", obj_Usuario.TELEFONO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AREA", obj_Usuario.AREA ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPARTAMENTO", obj_Usuario.DEPARTAMENTO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CARGO", obj_Usuario.CARGO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PERFIL", obj_Usuario.PERFIL ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@FIRMA", obj_Usuario.FIRMA ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IDEMPLEADO", obj_Usuario.IDEMPLEADO ?? (object)DBNull.Value);
                            object result = await cmd.ExecuteScalarAsync();
                            val = Convert.ToInt32(result);
                            //val = await cmd.ExecuteNonQueryAsync();
                        }

                        transaccion.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        // Aquí podrías registrar el error en logs o sistema de trazabilidad
                        throw new Exception("Error al grabar usuario: " + ex.Message, ex);
                    }
                } // La transacción se cierra automáticamente
            } // La conexión se cierra automáticamente

            return val;
        }

        

        public async Task<int> Actualizar_Usuarios(BE_Usuario obj_Usuario)
        {
            int val = 0;

            using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
            {
                await con.OpenAsync();

                using (SqlTransaction transaccion = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = con.CreateCommand())
                        {
                            cmd.Transaction = transaccion;
                            cmd.CommandText = "SP_ACTUALIZAR_USUARIO";
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@ID", obj_Usuario.ID);
                            cmd.Parameters.AddWithValue("@APELLIDOS", obj_Usuario.APELLIDOS ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NOMBRES", obj_Usuario.NOMBRES ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CORREO", obj_Usuario.CORREO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@USUARIO", obj_Usuario.USUARIO ?? (object)DBNull.Value);
                            //cmd.Parameters.AddWithValue("@CONTRASEÑA", obj_Usuario.CONTRASEÑA ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CONTRASEÑA", obj_Usuario.CONTRASEÑA ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@USUARIO_MODIFICACION", obj_Usuario.USUARIO_MODIFICACION ?? "SYSTEM");
                            cmd.Parameters.AddWithValue("@ESTADO", obj_Usuario.ESTADO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TELEFONO", obj_Usuario.TELEFONO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AREA", obj_Usuario.AREA ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPARTAMENTO", obj_Usuario.DEPARTAMENTO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CARGO", obj_Usuario.CARGO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PERFIL", obj_Usuario.PERFIL ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@FIRMA", obj_Usuario.FIRMA ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@FORZAR_CAMBIO_CLAVE", obj_Usuario.FORZAR_CAMBIO_CLAVE == "1" ? 1 : 0);
                            cmd.Parameters.AddWithValue("@FECHA_CAMBIO_CLAVE",
                            string.IsNullOrWhiteSpace(obj_Usuario.FECHA_CAMBIO_CLAVE)
                                ? (object)DBNull.Value
                                : Convert.ToDateTime(obj_Usuario.FECHA_CAMBIO_CLAVE));
                            cmd.Parameters.AddWithValue("@BLOQUEADO", obj_Usuario.BLOQUEADO);
                            cmd.Parameters.AddWithValue("@IDEMPLEADO", obj_Usuario.IDEMPLEADO ?? (object)DBNull.Value);

                            using (SqlDataReader lector = await cmd.ExecuteReaderAsync())
                            {
                                if (await lector.ReadAsync())
                                    val = lector.GetInt32(0);
                            }
                        }

                        transaccion.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        throw new Exception("Error al actualizar usuario: " + ex.Message, ex);
                    }
                }
            }

            return val;
        }

        public async Task<List<BE_Usuario>> Buscar_Usuarios(BE_Usuario obj_Usuario)
        {
            var lista = new List<BE_Usuario>();

            try
            {
                using (var conexion = new SqlConnection(_conexion.CadenaSQL))
                {
                    await conexion.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_BUSCAR_USUARIO", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@APELLIDOS", string.IsNullOrEmpty(obj_Usuario.APELLIDOS) ? "" : obj_Usuario.APELLIDOS.Trim());
                        cmd.Parameters.AddWithValue("@NOMBRES", string.IsNullOrEmpty(obj_Usuario.NOMBRES) ? "" : obj_Usuario.NOMBRES.Trim());
                        cmd.Parameters.AddWithValue("@ESTADO", string.IsNullOrEmpty(obj_Usuario.ESTADO) ? "" : obj_Usuario.ESTADO.Trim());
                        cmd.Parameters.AddWithValue("@USUARIO", string.IsNullOrEmpty(obj_Usuario.USUARIO) ? "" : obj_Usuario.USUARIO.Trim()); // Corregido aquí

                        using (var lector = await cmd.ExecuteReaderAsync())
                        {
                            while (await lector.ReadAsync())
                            {
                                lista.Add(new BE_Usuario()
                                {
                                    ID = Convert.ToInt32(lector[0]),
                                    APELLIDOS = lector[1].ToString().Trim(),
                                    NOMBRES = lector[2].ToString().Trim(),
                                    CORREO = lector[3].ToString().Trim(),
                                    USUARIO = lector[4].ToString().Trim(),
                                    CONTRASEÑA = lector[5].ToString().Trim(),
                                    FECHA_CREACION = lector[6].ToString().Trim(),
                                    USUARIO_CREACION = lector[7].ToString().Trim(),
                                    FECHA_MODIFICACION = lector[8].ToString().Trim(),
                                    USUARIO_MODIFICACION = lector[9].ToString().Trim(),
                                    ESTADO = lector[10].ToString().Trim()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al buscar usuarios: " + ex.Message, ex);
            }

            return lista;
        }

        public async Task<List<BE_Usuario>> Obtener_Usuarios(BE_Usuario obj_Usuario)
        {
            var lista = new List<BE_Usuario>();

            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_OBTENER_DATOS_USUARIO", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID", obj_Usuario.ID);

                        using (SqlDataReader lector = await cmd.ExecuteReaderAsync())
                        {
                            while (await lector.ReadAsync())
                            {
                                int? codVendedorSap = 0;
                                for (int i = 0; i < lector.FieldCount; i++)
                                {
                                    if (string.Equals(lector.GetName(i), "CODIGO_VENDEDOR_SAP", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!lector.IsDBNull(i))
                                        {
                                            codVendedorSap = Convert.ToInt32(lector.GetValue(i));
                                        }
                                        break;
                                    }
                                }

                                var obj_BE = new BE_Usuario
                                {
                                    ID = lector.IsDBNull(0) ? 0 : lector.GetInt32(0),
                                    APELLIDOS = lector.IsDBNull(1) ? "" : lector.GetString(1).Trim(),
                                    NOMBRES = lector.IsDBNull(2) ? "" : lector.GetString(2).Trim(),
                                    CORREO = lector.IsDBNull(3) ? "" : lector.GetString(3).Trim(),
                                    USUARIO = lector.IsDBNull(4) ? "" : lector.GetString(4).Trim(),
                                    //CONTRASEÑA = lector.IsDBNull(5) ? "" : lector.GetString(5).Trim(),
                                    CONTRASEÑA = "",
                                    FECHA_CREACION = lector[6].ToString().Trim(),
                                    USUARIO_CREACION = lector.IsDBNull(7) ? "" : lector.GetString(7).Trim(),
                                    FECHA_MODIFICACION = lector[8].ToString().Trim(),
                                    USUARIO_MODIFICACION = lector.IsDBNull(9) ? "" : lector.GetString(9).Trim(),
                                    ESTADO = lector.IsDBNull(10) ? "" : lector.GetString(10).Trim(),
                                    TELEFONO = lector.IsDBNull(11) ? "" : lector.GetString(11).Trim(),
                                    AREA = lector.IsDBNull(12) ? "" : lector.GetString(12).Trim(),
                                    DEPARTAMENTO = lector.IsDBNull(13) ? "" : lector.GetString(13).Trim(),
                                    CARGO = lector.IsDBNull(14) ? "" : lector.GetString(14).Trim(),
                                    PERFIL = lector.IsDBNull(15) ? "" : lector.GetString(15).Trim(),

                                    FIRMA = lector.IsDBNull(16) ? "" : lector.GetString(16).Trim(),
                                    FECHA_CAMBIO_CLAVE = lector.IsDBNull(17) ? "" : lector[17].ToString().Trim(),
                                    FORZAR_CAMBIO_CLAVE = lector.IsDBNull(18) ? "0" : lector[18].ToString().Trim(),
                                    IDEMPLEADO = lector.IsDBNull(19) ? (long?)null : lector.GetInt64(19),   // 👈 nuevo, índice 19
                                    ULTIMO_CAMBIO_CLAVE = lector.IsDBNull(20) ? "" : lector[20].ToString().Trim(),
                                    //ULTIMO_CAMBIO_CLAVE = lector.IsDBNull(19) ? "" : lector[19].ToString().Trim(),
                                    BLOQUEADO = Convert.ToBoolean(lector["BLOQUEADO"].ToString().Trim()),
                                    CODIGO_VENDEDOR_SAP = codVendedorSap
                                };

                                lista.Add(obj_BE);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener datos de usuario: " + ex.Message, ex);
            }

            return lista;
        }

        public async Task<BE_Empleado> ObtenerEmpleadoPorId(long idEmpleado)
        {
            BE_Empleado obj_BE = null;
            using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
            {
                await con.OpenAsync();
                try
                {
                    using (SqlCommand cmd = new SqlCommand("SP_OBTENER_EMPLEADO_POR_ID", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdEmpleado", idEmpleado);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                obj_BE = new BE_Empleado
                                {
                                    IDEMPLEADO = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                                    NOMBRECOMPLETO = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                                    NOMBRE = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim(),
                                    APELLIDOS = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim(),
                                    CARGO = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim(),
                                    DOCUMENTO = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim()
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al obtener empleado: " + ex.Message, ex);
                }
            }
            return obj_BE;
        }
        public async Task<int> Cambiar_Clave(BE_Usuario obj_Usuario)
        {
            int val = 0;

            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();

                    using (SqlTransaction transaccion = con.BeginTransaction())
                    {
                        using (SqlCommand cmd = transaccion.Connection.CreateCommand())
                        {
                            cmd.CommandText = "SP_ACTUALIZAR_CLAVE";
                            cmd.Transaction = transaccion;
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@ID", obj_Usuario.ID);
                            cmd.Parameters.AddWithValue("@CLAVE1", obj_Usuario.CLAVE);
                            cmd.Parameters.AddWithValue("@USUARIO", obj_Usuario.USUARIO);

                            val = await cmd.ExecuteNonQueryAsync();
                            cmd.Parameters.Clear();
                        }

                        transaccion.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                // Opcional: loggear error aquí si tienes logger.
                throw new Exception("Error al cambiar la clave del usuario: " + ex.Message, ex);
            }

            return val;
        }

        public async Task<int> Inactivar_Usuario(BE_Usuario obj_Usuario)
        {
            int val = 0;

            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();

                    using (SqlTransaction transaccion = con.BeginTransaction())
                    {
                        using (SqlCommand cmd = transaccion.Connection.CreateCommand())
                        {
                            cmd.CommandText = "SP_ELIMINAR_USUARIO";
                            cmd.Transaction = transaccion;
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@ID", obj_Usuario.ID);

                            val = await cmd.ExecuteNonQueryAsync();
                            cmd.Parameters.Clear();
                        }

                        transaccion.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                // Loguear error si es necesario
                throw new Exception("Error al inactivar usuario: " + ex.Message, ex);
            }

            return val;
        }

        public async Task<string> Actualizar_ImagenSolicitud(string id, string imgAdjunto)
        {
            try
            {
                await using var con = new SqlConnection(_conexion.CadenaSQL);
                await con.OpenAsync();

                await using var cmd = new SqlCommand("SP_ACTUALIZAR_IMAGEN_USUARIO", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ID", id.Trim());
                cmd.Parameters.AddWithValue("@IMG_ADJUNTO", imgAdjunto ?? (object)DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    bool respuesta = Convert.ToInt32(reader["Codigo"]) == 1;
                    string mensaje = reader["Mensaje"]?.ToString();
                    return $"{{\"respuesta\":{respuesta.ToString().ToLower()},\"mensaje\":\"{mensaje}\"}}";
                }

                return "{\"respuesta\":false,\"mensaje\":\"No se obtuvo respuesta del servidor\"}";
            }
            catch (Exception ex)
            {
                throw new Exception("Error en repositorio al actualizar imagen de solicitud: " + ex.Message, ex);
            }
        }
        public async Task<string> ObtenerRutaArchivo(string id)
        {
            string ruta = null;

            try
            {
                await using var con = new SqlConnection(_conexion.CadenaSQL);
                await con.OpenAsync();

                await using var cmd = new SqlCommand("SP_OBTENER_IMAGEN_USUARIO", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ID", id);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    ruta = reader["IMG_ADJUNTO"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener ruta de archivo: " + ex.Message, ex);
            }

            return ruta;
        }

        //Nuevo metodos para validacion de loguin
        public async Task<int> Cambiar_Clave_Segura(BE_CambioClave obj_Usuario)
        {
            int val = 0;
            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();
                    using (SqlTransaction transaccion = con.BeginTransaction())
                    {
                        using (SqlCommand cmd = transaccion.Connection.CreateCommand())
                        {
                            cmd.CommandText = "SP_CAMBIAR_CLAVE";
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Transaction = transaccion;
                            cmd.Parameters.AddWithValue("@ID_USUARIO", Convert.ToInt32(obj_Usuario.ID));
                            cmd.Parameters.AddWithValue("@CLAVE_NUEVA", EncriptacionHelper.EncryptPlainTextToCipherText(obj_Usuario.CLAVE_NUEVA));
                            cmd.Parameters.AddWithValue("@USUARIO_MOD", obj_Usuario.USUARIO_MODIFICACION ?? "SYSTEM");
                            cmd.Parameters.AddWithValue("@FECHA_CAMBIO_CLAVE", 
                                string.IsNullOrWhiteSpace(obj_Usuario.FECHA_CAMBIO_CLAVE) 
                                    ? (object)DBNull.Value 
                                    : (DateTime.TryParse(obj_Usuario.FECHA_CAMBIO_CLAVE, out var dt) ? (object)dt : DBNull.Value));

                            using (SqlDataReader lector = await cmd.ExecuteReaderAsync())
                            {
                                if (await lector.ReadAsync())
                                    val = lector.GetInt32(0);
                            }
                            cmd.Parameters.Clear();
                        }
                        transaccion.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cambiar la clave del usuario: " + ex.Message, ex);
            }
            return val;
        }

        public async Task<List<string>> Obtener_Historial_Claves(BE_CambioClave obj)
        {
            var lista = new List<string>();

            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_OBTENER_HISTORIAL_CLAVES", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID_USUARIO", Convert.ToInt32(obj.ID));

                        using (SqlDataReader lector = await cmd.ExecuteReaderAsync())
                        {
                            while (await lector.ReadAsync())
                            {
                                lista.Add(lector[0].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener historial de claves: " + ex.Message, ex);
            }

            return lista;
        }

        public async Task<string> Obtener_Clave_Usuario(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_OBTENER_CLAVE_USUARIO", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID", id);
                        var result = await cmd.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener clave: " + ex.Message, ex);
            }
        }

        //public async Task<int> Forzar_Cambio_Clave_Todos(BE_Usuario obj_Usuario)
        //{
        //    int val = 0;

        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
        //        {
        //            await con.OpenAsync();

        //            using (SqlTransaction transaccion = con.BeginTransaction())
        //            {
        //                using (SqlCommand cmd = transaccion.Connection.CreateCommand())
        //                {
        //                    cmd.CommandText = "SP_FORZAR_CAMBIO_CLAVE_TODOS";
        //                    cmd.Transaction = transaccion;
        //                    cmd.CommandType = CommandType.StoredProcedure;

        //                    cmd.Parameters.AddWithValue("@USUARIO_MOD", obj_Usuario.USUARIO_MODIFICACION ?? "SYSTEM");

        //                    val = await cmd.ExecuteNonQueryAsync();
        //                    cmd.Parameters.Clear();
        //                }

        //                transaccion.Commit();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error al forzar cambio de clave: " + ex.Message, ex);
        //    }

        //    return val;
        //}
        public async Task<List<BE_Empleado>> Autocompletar_Empleado(BE_Empleado empleado)
        {
            var lista = new List<BE_Empleado>();

            using(SqlConnection con=new SqlConnection(_conexion.CadenaSQL))
            {
                await con.OpenAsync();
                try
                {
                    using (SqlCommand cmd = new SqlCommand("SP_BUSCAR_EMPLEADOS_USUARIOS", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Nombres", empleado.NOMBRE);
                        using (SqlDataReader reader =await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var obj_BE = new BE_Empleado
                                {
                                    IDEMPLEADO = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                                    NOMBRECOMPLETO = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                                    NOMBRE = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim(),
                                    APELLIDOS = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim(),
                                    CARGO = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim(),
                                    AREA = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim(),
                                    DOCUMENTO = reader.IsDBNull(6) ? "" : reader.GetString(6).Trim(),
                                    TIENE_USUARIO = !reader.IsDBNull(7) && reader.GetInt32(7) == 1
                                };
                                lista.Add(obj_BE);
                            }

                        }

                    }
                }catch(Exception ex)
                {
                    throw new Exception("Error al obtener: " + ex.Message, ex);
                }
                return lista;
            }
        }
    }
}
