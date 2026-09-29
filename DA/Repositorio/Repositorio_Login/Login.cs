using BE;
using DA.Configuracion;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Login
{
    public class Login : ILogin
    {
        private readonly ConfiguracionConexion _conexion;
        public Login(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
        }


        public async Task<List<BE_Usuario>> Validar_Login(BE_Usuario obj_Usuario)
        {
            List<BE_Usuario> lista = new List<BE_Usuario>();
            using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
            {
                await con.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("SP_VALIDAR_LOGIN", con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO", obj_Usuario.USUARIO);
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader lector = await cmd.ExecuteReaderAsync())
                    {
                        while (await lector.ReadAsync())
                        {
                            BE_Usuario obj_BE = new BE_Usuario
                            {
                                ID = Convert.ToInt32(lector[0].ToString().Trim()),
                                APELLIDOS = lector[1].ToString().Trim(),
                                NOMBRES = lector[2].ToString().Trim(),
                                CORREO = lector[3].ToString().Trim(),
                                USUARIO = lector[4].ToString().Trim(),
                                CONTRASEÑA = lector[5].ToString().Trim(),
                                FECHA_CREACION = lector[6].ToString().Trim(),
                                USUARIO_CREACION = lector[7].ToString().Trim(),
                                FECHA_MODIFICACION = lector[8].ToString().Trim(),
                                USUARIO_MODIFICACION = lector[9].ToString().Trim(),
                                ESTADO = lector[10].ToString().Trim(),
                                TELEFONO = lector[11].ToString().Trim(),
                                AREA = lector[12].ToString().Trim(),
                                DEPARTAMENTO = lector[13].ToString().Trim(),
                                PERFIL = lector[14].ToString().Trim(),
                                CARGO = lector[15].ToString().Trim(),
                                //FECHA_CAMBIO_CLAVE = lector[16].ToString().Trim(),
                                //FORZAR_CAMBIO_CLAVE = lector[17].ToString().Trim(),
                                BLOQUEADO = Convert.ToBoolean(lector["BLOQUEADO"].ToString().Trim()),
                                INTENTOS = Convert.ToInt32(lector["INTENTOS"].ToString().Trim()),
                                FECHA_CAMBIO_CLAVE = lector[16].ToString().Trim(),   // nuevo
                                FORZAR_CAMBIO_CLAVE = lector[17].ToString().Trim(),    // nuevo
                                CODIGO_VENDEDOR_SAP = lector.IsDBNull(18)
                                    ? 0
                                    : Convert.ToInt32(lector[18])
                            };
                            lista.Add(obj_BE);
                        }
                    }
                }
            }
            return lista;
        }
        public async Task<BE_Usuario> ControlIntentosLogin(int id, bool loginCorrecto)
        {
            BE_Usuario usuario = null;

            using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("SP_OBTENER_INTENTOS_LOGIN", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.Parameters.AddWithValue("@LOGIN_CORRECTO", loginCorrecto);

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            usuario = new BE_Usuario
                            {
                                INTENTOS = Convert.ToInt32(dr["INTENTOS"]),
                                BLOQUEADO = Convert.ToBoolean(dr["BLOQUEADO"])
                            };
                        }
                    }
                }
            }

            return usuario;
        }

    }
}
