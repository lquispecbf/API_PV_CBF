using BE;
using DA.Configuracion;
using Microsoft.Extensions.Options;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace DA.Repositorio.Repositorio_Extranet
{
    public class CambioClave : ICambioClave
    {
        private readonly ConfiguracionConexion _conexion;
        public CambioClave(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
        }

        public async Task<List<BE_UsuarioExtranet>> Buscar_Usuarios(BE_UsuarioExtranet obj_Usuario)
        {
            var lista = new List<BE_UsuarioExtranet>();

            try
            {
                using (var conexion = new SqlConnection(_conexion.CadenaSQLExt))
                {
                    await conexion.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_BUSCAR_USUARIOS", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@NOMBRE", SqlDbType.VarChar, 90).Value = string.IsNullOrWhiteSpace(obj_Usuario.NombreUsuario) ? DBNull.Value : obj_Usuario.NombreUsuario.Trim();
                        cmd.Parameters.Add("@CLIENTE", SqlDbType.VarChar, 90).Value = string.IsNullOrWhiteSpace(obj_Usuario.CLIENTE_Ruc) ? DBNull.Value : obj_Usuario.CLIENTE_Ruc.Trim();
                        cmd.Parameters.Add("@CORREO", SqlDbType.VarChar, 1000).Value = string.IsNullOrWhiteSpace(obj_Usuario.CorreoElectronico) ? DBNull.Value : obj_Usuario.CorreoElectronico.Trim();
                        cmd.Parameters.Add("@PERFIL", SqlDbType.VarChar, 200).Value = string.IsNullOrWhiteSpace(obj_Usuario.Perfil) ? DBNull.Value : obj_Usuario.Perfil.Trim();
                        cmd.Parameters.Add("@ESTADO", SqlDbType.Int).Value = obj_Usuario.Estado.HasValue ? obj_Usuario.Estado.Value : DBNull.Value;
                        cmd.Parameters.Add("@NOMBREPERSONA", SqlDbType.VarChar, 400).Value = string.IsNullOrWhiteSpace(obj_Usuario.NombrePersona) ? DBNull.Value : obj_Usuario.NombrePersona.Trim();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new BE_UsuarioExtranet()
                                {
                                    IdUsuario = reader["IdUsuario"] as int?,
                                    NombreUsuario = reader["NombreUsuario"] as string,
                                    CLIENTE_Ruc = reader["CLIENTE_Ruc"] as string,
                                    CorreoElectronico = reader["CorreoElectronico"] as string,
                                    IdPerfil = reader["IdPerfil"] as int?,
                                    Perfil = reader["Perfil"] as string,
                                    Estado = reader["Estado"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Estado"]),
                                    NombrePersona = reader["NombrePersona"] as string
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

        public async Task<List<BE_UsuarioExtranet>> Buscar_Usuario(BE_UsuarioExtranet obj_Usuario)
        {
            var lista = new List<BE_UsuarioExtranet>();

            try
            {
                using (var conexion = new SqlConnection(_conexion.CadenaSQLExt))
                {
                    await conexion.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_BUSCAR_USUARIO", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = obj_Usuario.IdUsuario.HasValue ? obj_Usuario.IdUsuario.Value : DBNull.Value;
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new BE_UsuarioExtranet()
                                {
                                    IdUsuario = reader["IdUsuario"] as int?,
                                    NombreUsuario = reader["NombreUsuario"] as string,
                                    ClaveUsuario = DecryptCipherTextToPlainText(reader["ClaveUsuario"].ToString()),
                                    CLIENTE_Ruc = reader["CLIENTE_Ruc"] as string,
                                    CorreoElectronico = reader["CorreoElectronico"] as string,
                                    IdPerfil = reader["IdPerfil"] as int?,
                                    Perfil = reader["Perfil"] as string,
                                    Estado = reader["Estado"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Estado"]),
                                    NombrePersona = reader["NombrePersona"] as string
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

        public async Task<int> Actualizar_Clave(BE_UsuarioExtranet obj_Usuario)
        {
            int val = 0;

            // Aquí la conexión siempre se cerrará al finalizar este bloque, incluso si ocurre una excepción.
            using (SqlConnection con = new SqlConnection(_conexion.CadenaSQLExt))
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
                            cmd.CommandText = "SP_ACTUALIZAR_CLAVE_USUARIO";
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@IdUsuario", obj_Usuario.IdUsuario);
                            cmd.Parameters.AddWithValue("@ClaveUsuario", EncryptPlainTextToCipherText(obj_Usuario.ClaveUsuario));

                            val = await cmd.ExecuteNonQueryAsync();
                        }

                        transaccion.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        // Aquí podrías registrar el error en logs o sistema de trazabilidad
                        throw new Exception("Error al actualizar clave de usuario: " + ex.Message, ex);
                    }
                }
            }

            return val;
        }

        public async Task<List<BE_ListadoPerfil>> Listar_Perfiles()
        {
            var lista = new List<BE_ListadoPerfil>();

            try
            {
                using (var conexion = new SqlConnection(_conexion.CadenaSQLExt))
                {
                    await conexion.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_LISTADO_PERFILES", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new BE_ListadoPerfil()
                                {
                                    idPerfil = reader["idPerfil"] as int?,
                                    NombrePerfil = reader["NombrePerfil"] as string
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar perfiles: " + ex.Message, ex);
            }

            return lista;
        }

        public static string EncryptPlainTextToCipherText(string PlainText)
        {
            byte[] toEncryptedArray = Encoding.UTF8.GetBytes(PlainText);
            using var objMD5 = MD5.Create();
            byte[] securityKeyArray = objMD5.ComputeHash(Encoding.UTF8.GetBytes("C0p17@2017"));
            using var objTripleDES = TripleDES.Create();
            objTripleDES.Key = securityKeyArray;
            objTripleDES.Mode = CipherMode.ECB;
            objTripleDES.Padding = PaddingMode.PKCS7;
            using var objCrytpoTransform = objTripleDES.CreateEncryptor();
            byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptedArray, 0, toEncryptedArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string DecryptCipherTextToPlainText(string CipherText)
        {
            byte[] toEncryptArray = Convert.FromBase64String(CipherText);
            using var objMD5 = MD5.Create();
            byte[] securityKeyArray = objMD5.ComputeHash(Encoding.UTF8.GetBytes("C0p17@2017"));
            using var objTripleDES = TripleDES.Create();
            objTripleDES.Key = securityKeyArray;
            objTripleDES.Mode = CipherMode.ECB;
            objTripleDES.Padding = PaddingMode.PKCS7;
            using var objCrytpoTransform = objTripleDES.CreateDecryptor();
            byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }

        public static bool ValidarClave(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return false;
            if (clave.Length < 8) return false;

            bool tieneMayuscula = clave.Any(char.IsUpper);
            bool tieneMinuscula = clave.Any(char.IsLower);
            bool tieneNumero = clave.Any(char.IsDigit);
            bool tieneCaracterEspecial = clave.Any(c => !char.IsLetterOrDigit(c));

            return tieneMayuscula && tieneMinuscula && tieneNumero && tieneCaracterEspecial;
        }
    }
}
