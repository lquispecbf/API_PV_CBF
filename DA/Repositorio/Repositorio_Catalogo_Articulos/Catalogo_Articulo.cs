using BE;
using DA.Configuracion;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Catalogo_Articulos
{
    public class Catalogo_Articulo : ICatalogo_Articulo
    {
        private readonly ConfiguracionConexion _conexion;
        private readonly string _companyDB;

        public Catalogo_Articulo(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
            _companyDB = _conexion.CompanyDB;
        }

        public async Task<int> ActualizarCatalogoArticulo(BE_Catalogo_Articulo obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            int resultado = 0;

            try
            {
                using (SqlConnection con = new SqlConnection(_conexion.CadenaSQL))
                {
                    await con.OpenAsync();

                    using (SqlTransaction transaccion = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("SP_ACTUALIZAR_CATALOGO_ARTICULOS", con, transaccion))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.Add(new SqlParameter("@CODIGO", SqlDbType.VarChar) { Value = obj.CODIGO });
                                cmd.Parameters.Add(new SqlParameter("@DESCRIPCION", SqlDbType.VarChar) { Value = obj.DESCRIPCION });
                                cmd.Parameters.Add(new SqlParameter("@IMG_NOMBRE_ORIGINAL", SqlDbType.VarChar) { Value = obj.IMG_NOMBRE_ORIGINAL });
                                cmd.Parameters.Add(new SqlParameter("@IMG_NOMBRE_GENERADO", SqlDbType.VarChar) { Value = obj.IMG_NOMBRE_GENERADO });
                                cmd.Parameters.Add(new SqlParameter("@IMG_RUTA", SqlDbType.VarChar) { Value = obj.IMG_RUTA });
                                cmd.Parameters.Add(new SqlParameter("@ESTADO", SqlDbType.VarChar) { Value = obj.ESTADO });

                                resultado = await cmd.ExecuteNonQueryAsync();
                            }

                            await transaccion.CommitAsync();
                        }
                        catch
                        {
                            await transaccion.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al Actualizar el catálogo del artículo", ex);
            }

            return resultado;
        }

        public async Task<(List<BE_Catalogo_Articulo>, int)> Buscar_CatalogoProductos(BE_Catalogo_Articulo obj, int pageNumber, int pageSize)
        {
            List<BE_Catalogo_Articulo> lista = new List<BE_Catalogo_Articulo>();
            int totalRegistros = 0;
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_BUSCAR_CATALOGO_ARTICULO", conexion);

                cmd.Parameters.AddWithValue("@CODIGO", string.IsNullOrEmpty(obj.CODIGO) ? "" : obj.CODIGO.Trim());
                cmd.Parameters.AddWithValue("@DESCRIPCION", string.IsNullOrEmpty(obj.DESCRIPCION) ? "" : obj.DESCRIPCION.Trim());
                cmd.Parameters.AddWithValue("@ESTADO", string.IsNullOrEmpty(obj.ESTADO) ? "" : obj.ESTADO.Trim());
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Catalogo_Articulo()
                        {
                            CODIGO = lector[0].ToString().Trim(),
                            DESCRIPCION = lector[1].ToString().Trim(),
                            PRINCIPIO_ACTIVO = lector[2].ToString().Trim(),
                            ESTADO = lector[3].ToString().Trim()
                        });
                    }
                    if (await lector.NextResultAsync())
                    {
                        if (await lector.ReadAsync())
                        {
                            totalRegistros = Convert.ToInt32(lector["TotalRecords"]);
                        }
                    }

                    lector.Close();
                }
            }
            return (lista, totalRegistros);
        }

        public async Task<List<BE_Catalogo_Articulo>> Buscar_Catalogo_Articulos_Tipo(BE_Catalogo_Articulo obj)
        {
            List<BE_Catalogo_Articulo> lista = new List<BE_Catalogo_Articulo>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_BUSCAR_CATALOGO_ARTICULOS_TIPO", conexion);

                cmd.Parameters.AddWithValue("@BUSCAR", string.IsNullOrEmpty(obj.BUSCAR_ARTICULO) ? "" : obj.BUSCAR_ARTICULO.Trim());
                cmd.Parameters.AddWithValue("@TIPO", string.IsNullOrEmpty(obj.TIPO) ? "" : obj.TIPO.Trim());

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Catalogo_Articulo()
                        {
                            CODIGO = lector[0].ToString().Trim(),
                            DESCRIPCION = lector[1].ToString().Trim(),
                            PRINCIPIO_ACTIVO = lector[2].ToString().Trim(),
                            ESTADO = lector[3].ToString().Trim(),
                            IMG_NOMBRE_ORIGINAL = lector[4].ToString().Trim(),
                            IMG_NOMBRE_GENERADO = lector[5].ToString().Trim(),
                            IMG_RUTA = lector[6].ToString().Trim()
                        });
                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<int> GrabarCatalogoArticulo(BE_Catalogo_Articulo obj)
        {
            int val = 0;
            SqlConnection con = new SqlConnection(_conexion.CadenaSQL);
            SqlTransaction transaccion = null;

            try
            {
                await con.OpenAsync();
                transaccion = con.BeginTransaction();

                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SP_INSERTAR_CATALOGO_ARTICULO";
                    cmd.Transaction = transaccion;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CODIGO", string.IsNullOrEmpty(obj.CODIGO) ? "" : obj.CODIGO.Trim());
                    cmd.Parameters.AddWithValue("@DESCRIPCION", string.IsNullOrEmpty(obj.DESCRIPCION) ? "" : obj.DESCRIPCION.Trim());
                    cmd.Parameters.AddWithValue("@IMG_NOMBRE_ORIGINAL", string.IsNullOrEmpty(obj.IMG_NOMBRE_ORIGINAL) ? "" : obj.IMG_NOMBRE_ORIGINAL.Trim());
                    cmd.Parameters.AddWithValue("@IMG_NOMBRE_GENERADO", string.IsNullOrEmpty(obj.IMG_NOMBRE_GENERADO) ? "" : obj.IMG_NOMBRE_GENERADO.Trim());
                    cmd.Parameters.AddWithValue("@IMG_RUTA", string.IsNullOrEmpty(obj.IMG_RUTA) ? "" : obj.IMG_RUTA.Trim());
                    cmd.Parameters.AddWithValue("@ESTADO", string.IsNullOrEmpty(obj.ESTADO) ? "" : obj.ESTADO.Trim());

                    val = await cmd.ExecuteNonQueryAsync();
                    cmd.Parameters.Clear();
                }

                transaccion.Commit();
            }
            catch (Exception ex)
            {
                if (transaccion != null)
                {
                    transaccion.Rollback();
                }
                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return val;
        }

        public async Task<int> Inactivar_Catalogo_Articulo(BE_Catalogo_Articulo obj)
        {
            int val = 0;
            SqlConnection con = new SqlConnection(_conexion.CadenaSQL);
            SqlTransaction transaccion = null;

            try
            {
                await con.OpenAsync();
                transaccion = con.BeginTransaction();

                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SP_INACTIVAR_CATALOGO_ARTICULO";
                    cmd.Transaction = transaccion;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CODIGO", string.IsNullOrEmpty(obj.CODIGO) ? "" : obj.CODIGO.Trim());
                    cmd.Parameters.AddWithValue("@ESTADO", string.IsNullOrEmpty(obj.ESTADO) ? "" : obj.ESTADO.Trim());

                    val = await cmd.ExecuteNonQueryAsync();
                    cmd.Parameters.Clear();
                }

                transaccion.Commit();
            }
            catch (Exception ex)
            {
                if (transaccion != null)
                {
                    transaccion.Rollback();
                }
                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return val;
        }

        public async Task<List<BE_Catalogo_Articulo>> Obtener_Datos_CatalogoProductos(BE_Catalogo_Articulo obj)
        {
            List<BE_Catalogo_Articulo> lista = new List<BE_Catalogo_Articulo>();
            using (var conexion = new SqlConnection(_conexion.CadenaSQL))
            {
                await conexion.OpenAsync();

                SqlCommand cmd = new SqlCommand("SP_OBTENER_DATOS_CATALOGO_ARTICULO", conexion);

                cmd.Parameters.AddWithValue("@CODIGO", string.IsNullOrEmpty(obj.CODIGO) ? "" : obj.CODIGO.Trim());

                cmd.CommandType = CommandType.StoredProcedure;

                using (var lector = await cmd.ExecuteReaderAsync())
                {
                    while (await lector.ReadAsync())
                    {
                        lista.Add(new BE_Catalogo_Articulo()
                        {
                            CODIGO = lector[0].ToString().Trim(),
                            DESCRIPCION = lector[1].ToString().Trim(),
                            PRINCIPIO_ACTIVO = lector[2].ToString().Trim(),
                            ESTADO = lector[3].ToString().Trim(),
                            IMG_NOMBRE_ORIGINAL = lector[4].ToString().Trim(),
                            IMG_NOMBRE_GENERADO = lector[5].ToString().Trim(),
                            IMG_RUTA = lector[6].ToString().Trim()
                        });
                    }
                    lector.Close();
                }
            }
            return lista;
        }

        public async Task<List<BE_Catalogo_Articulo>> ObtenerAutoCompletadoProducto(BE_Catalogo_Articulo obj)
        {
            List<BE_Catalogo_Articulo> lista = new();

            string cn = _conexion.CadenaSAP_ODBC;

            try
            {
                using (OdbcConnection conn = new OdbcConnection(cn))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT DISTINCT
                            T0.""ItemCode"",
                            T0.""ItemName"",
                            IFNULL(T0.""U_SYP_PRINACT"", '') AS ""PrincipioActivo"",
                            CASE WHEN T0.""validFor"" = 'Y' THEN 'Activo' ELSE 'Inactivo' END AS ""Estado""
                        FROM " + _companyDB + @"""OITM"" T0
                        WHERE (
                            UPPER(T0.""ItemCode"") LIKE UPPER(?) OR
                            UPPER(T0.""ItemName"") LIKE UPPER(?) OR
                            UPPER(IFNULL(T0.""U_SYP_PRINACT"", '')) LIKE UPPER(?)
                        )
                        ORDER BY T0.""ItemName""
                        LIMIT 50";

                    using (OdbcCommand cmd = new OdbcCommand(query, conn))
                    {
                        string buscar = $"%{obj.BUSCAR_ARTICULO?.Trim()}%";

                        cmd.Parameters.Add(new OdbcParameter("@p1", OdbcType.VarChar) { Value = buscar });
                        cmd.Parameters.Add(new OdbcParameter("@p2", OdbcType.VarChar) { Value = buscar });
                        cmd.Parameters.Add(new OdbcParameter("@p3", OdbcType.VarChar) { Value = buscar });

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new BE_Catalogo_Articulo
                                {
                                    CODIGO = reader["ItemCode"].ToString(),
                                    DESCRIPCION = reader["ItemName"].ToString(),
                                    PRINCIPIO_ACTIVO = reader["PrincipioActivo"].ToString(),
                                    ESTADO = reader["Estado"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener autocompletado desde SAP HANA", ex);
            }

            return lista;
        }
    }
}
