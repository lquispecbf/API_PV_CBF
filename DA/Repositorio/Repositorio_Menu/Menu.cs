using BE;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DA.Configuracion;
using Microsoft.Extensions.Options;

namespace DA.Repositorio.Repositorio_Menu
{
    public class Menu : IMenu
    {
        private readonly ConfiguracionConexion _conexion;
        public Menu(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
        }

        public async Task<List<BE_Usuario>> Buscar_Usuario_Permiso(BE_Usuario obj_Usuario)
        {
            List<BE_Usuario> lista = new List<BE_Usuario>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_BUSCAR_USUARIO", conexion);

                cmd.Parameters.AddWithValue("@APELLIDOS", string.IsNullOrEmpty(obj_Usuario.APELLIDOS) ? "" : obj_Usuario.APELLIDOS.Trim());
                cmd.Parameters.AddWithValue("@NOMBRES", string.IsNullOrEmpty(obj_Usuario.NOMBRES) ? "" : obj_Usuario.NOMBRES.Trim());
                cmd.Parameters.AddWithValue("@ESTADO", string.IsNullOrEmpty(obj_Usuario.ESTADO) ? "" : obj_Usuario.ESTADO.Trim());
                cmd.Parameters.AddWithValue("@USUARIO", string.IsNullOrEmpty(obj_Usuario.ESTADO) ? "" : obj_Usuario.USUARIO.Trim());
                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Usuario()
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
                            ESTADO = lector[10].ToString().Trim()
                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<int> Guardar_Permiso(BE_Usuario obj_Usuario)
        {
            var dt = new DataTable();
            dt.Columns.Add("Id_Usuario", typeof(int));
            dt.Columns.Add("Id_Permisos", typeof(int));
            dt.Columns.Add("Usuario_Creacion", typeof(string));

            foreach (var item in obj_Usuario.PERMISOS)
            {
                dt.Rows.Add(obj_Usuario.ID, item, obj_Usuario.USUARIO_CREACION);
            }

            int val = 0;
            SqlConnection con = new SqlConnection(_conexion.CadenaSQL);
            SqlTransaction transaccion = null;

            try
            {
                await con.OpenAsync();
                transaccion = con.BeginTransaction();

                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SP_INSERTAR_PERMISOS";
                    cmd.Transaction = transaccion;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Usuario", obj_Usuario.ID);

                    SqlParameter tvpParam = cmd.Parameters.AddWithValue("@LISTA_PERMISOS", dt);
                    tvpParam.SqlDbType = SqlDbType.Structured;

                    val = await cmd.ExecuteNonQueryAsync();
                    cmd.Parameters.Clear();
                }

                transaccion.Commit();
            }
            catch (Exception ex)
            {
                transaccion?.Rollback();
                // Puedes loguear el error si usas algún sistema de logs
                throw new Exception("Error al guardar los permisos del usuario.", ex);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            return val;
        }


        public async Task<List<BE_Menu>> Listar_Menu()
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS", conexion);


                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ID_MENU_PADRE = Convert.ToInt32(lector[2].ToString().Trim()),
                            NIVEL_MENU = Convert.ToInt32(lector[3].ToString().Trim()),
                            ORDEN_MENU = Convert.ToInt32(lector[4].ToString().Trim()),
                            ESTADO_MENU = lector[5].ToString().Trim(),

                        });


                    }
                    lector.Close();
                }
            }
            List<BE_Menu> lista_final = new List<BE_Menu>();


            for (var i = 0; i < lista.Count; i++)
            {
                if (lista[i].ID_MENU_PADRE == 0 && lista[i].NIVEL_MENU == 1)
                {
                    int ID_MENU_NIVEL1 = lista[i].ID_MENU;
                    string DESCRIPCION_MENU_PADRE_NIVEL1 = lista[i].DESCRIPCION_MENU.ToString().Trim();

                    lista_final.Add(new BE_Menu()
                    {
                        DESCRIPCION_MENU = lista[i].DESCRIPCION_MENU.ToString().Trim(),
                    });

                    for (var j = 0; j < lista.Count; j++)
                    {
                        if (lista[j].ID_MENU_PADRE == ID_MENU_NIVEL1 && lista[j].NIVEL_MENU == 2)
                        {
                            int ID_MENU_NIVEL2 = lista[j].ID_MENU;
                            string DESCRIPCION_MENU_PADRE_NIVEL2 = lista[i].DESCRIPCION_MENU.ToString().Trim();

                            lista_final.Add(new BE_Menu()
                            {

                                DESCRIPCION_MENU = lista[j].DESCRIPCION_MENU.ToString().Trim(),


                            });

                            for (var k = 0; k < lista.Count; k++)
                            {
                                if (lista[k].ID_MENU_PADRE == ID_MENU_NIVEL2 && lista[k].NIVEL_MENU == 3)
                                {
                                    int ID_MENU_NIVEL3 = lista[k].ID_MENU;
                                    string DESCRIPCION_MENU_PADRE_NIVEL3 = lista[i].DESCRIPCION_MENU.ToString().Trim();
                                    lista_final.Add(new BE_Menu()
                                    {
                                        DESCRIPCION_MENU = lista[k].DESCRIPCION_MENU.ToString().Trim(),
                                    });



                                    for (var p = 0; p < lista.Count; p++)
                                    {
                                        if (lista[p].ID_MENU_PADRE == ID_MENU_NIVEL3 && lista[p].NIVEL_MENU == 4)
                                        {

                                            lista_final.Add(new BE_Menu()
                                            {
                                                DESCRIPCION_MENU = lista[p].DESCRIPCION_MENU.ToString().Trim(),
                                            });



                                            for (var s = 0; s < lista.Count; s++)
                                            {
                                                if (lista[s].ID_MENU_PADRE == ID_MENU_NIVEL3 && lista[s].NIVEL_MENU == 14)
                                                {

                                                    lista_final.Add(new BE_Menu()
                                                    {
                                                        DESCRIPCION_MENU = lista[p].DESCRIPCION_MENU.ToString().Trim(),
                                                    });

                                                }
                                            }



                                        }
                                    }

                                }
                            }



                        }
                    }



                }
            }



            return lista_final;

        }

        public async Task<List<BE_Menu>> Listar_Permisos_Mantenimiento(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_MANTENIMIENTO", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim()

                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Mastercard(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_MASTERCARD", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim(),

                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Requerimientos(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_REQUERIMIENTO", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim()

                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Lista_Permisos_Recursos(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_RECURSOS", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Lista_Permisos_Comercial(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_COMERCIAL", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }
        public async Task<List<BE_Menu>> Lista_Permisos_Finanzas(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_FINANZAS", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Macador(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_MARCADOR", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim()

                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Seguridad(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_SEGURIDAD", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim(),

                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }
        public async Task<List<BE_Menu>> Listar_Permisos_Operaciones(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_OPERACIONES", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }
        public async Task<List<BE_Menu>> Listar_Permisos_Reportes(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_REPORTES", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }
        public async Task<List<BE_Menu>> Listar_Permisos_PuntoVenta(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_PUNTO_VENTA", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Sistemas(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_SISTEMAS", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Administracion(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_ADMINISTRACION", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }

        public async Task<List<BE_Usuario>> Mostrar_Menu(BE_Usuario obj_Usuario)
        {
            List<BE_Usuario> lista = new List<BE_Usuario>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENU_USUARIO", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Usuario()
                        {
                            DESCRIPCION_MENU_SYS = lector[0].ToString().Trim()
                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<List<BE_Menu>> Listar_Permisos_Facturacion(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                conexion.Open();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_FACTURACION", conexion);
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector[0].ToString().Trim()),
                            DESCRIPCION_MENU = lector[1].ToString().Trim(),
                            ESTADO_MENU = lector[2].ToString().Trim()

                        });


                    }
                    lector.Close();
                }
            }
            return lista;
        }
        public async Task<List<BE_Menu>> Lista_Permisos_DireccionTecnica(BE_Usuario obj_Usuario)
        {
            List<BE_Menu> lista = new List<BE_Menu>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS_DIRECCIONTECNICA", conexion);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_USUARIO", obj_Usuario.ID);

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Menu()
                        {
                            ID_MENU = Convert.ToInt32(lector["ID_MENU"]),
                            DESCRIPCION_MENU = lector["DESCRIPCION_MENU"].ToString().Trim(),
                            ESTADO_MENU = lector["ESTADO"].ToString().Trim(),
                            GRUPO_MENU = lector["GRUPO_MENU"].ToString().Trim()
                        });
                    }
                }
            }
            return lista;
        }
        public async Task<bool> Validar_Permiso_Usuario_Accion(int idUsuario, string controller, string action)
        {
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                using (var cmd = new SqlCommand("SP_VALIDAR_PERMISO_USUARIO_ACCION", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ID_USUARIO", idUsuario);
                    cmd.Parameters.AddWithValue("@CONTROLLER_MENU", controller ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ACTION_MENU", action ?? string.Empty);

                    var resultado = await cmd.ExecuteScalarAsync();

                    if (resultado == null || resultado == DBNull.Value)
                        return false;

                    return Convert.ToInt32(resultado) == 1;
                }
            }
        }
    }

}
