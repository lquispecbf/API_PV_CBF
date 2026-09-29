using System.Data;
using System.Data.Common;
using BE;
using BE.Auditoria;
using DA.AccesoDatos;
using DA.Configuracion;

namespace DA.Repositorio.Repositorio_Auditoria;

public class Auditoria : IAuditoria
{
    private readonly ISqlExecutor _sql;

    public Auditoria(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task RegistrarAuditoria(BE_Auditoria auditoria)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@FechaHora", auditoria.FECHA_HORA),
            _sql.CreateParameter("@Usuario", auditoria.USUARIO),
            _sql.CreateParameter("@UsuarioId", auditoria.USUARIO_ID),

            _sql.CreateParameter("@Ip", auditoria.IP),
            _sql.CreateParameter("@UserAgent", auditoria.USER_AGENT),

            _sql.CreateParameter("@SistemaOperativo", auditoria.SISTEMA_OPERATIVO),
            _sql.CreateParameter("@Dispositivo", auditoria.DISPOSITIVO),
            _sql.CreateParameter("@Navegador", auditoria.NAVEGADOR),

            _sql.CreateParameter("@Controller", auditoria.CONTROLLER),
            _sql.CreateParameter("@Action", auditoria.ACTION),

            _sql.CreateParameter("@Url", auditoria.URL),
            _sql.CreateParameter("@QueryString", auditoria.QUERY_STRING),

            _sql.CreateParameter("@MetodoHttp", auditoria.METODO_HTTP),

            _sql.CreateParameter("@TipoAccion", auditoria.TIPO_ACCION),

            _sql.CreateParameter("@CodigoRespuesta", auditoria.CODIGO_RESPUESTA),

            _sql.CreateParameter("@TiempoMs", auditoria.TIEMPO_MS),

            _sql.CreateParameter("@Vista", auditoria.VISTA),

            _sql.CreateParameter("@NombreArchivo", auditoria.NOMBRE_ARCHIVO),

            _sql.CreateParameter("@Parametros", auditoria.PARAMETROS),

            _sql.CreateParameter("@Excepcion", auditoria.EXCEPCION),

            _sql.CreateParameter("@Origen", auditoria.ORIGEN ?? (object)"INTRANET"),
            _sql.CreateParameter("@Latitud", auditoria.LATITUD ?? (object)DBNull.Value),
            _sql.CreateParameter("@Longitud", auditoria.LONGITUD ?? (object)DBNull.Value)
        };

        await _sql.QuerySingleAsync(
            "SP_SEG_AUDITORIA_INSERTAR",
            MapRespuestaBD,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters
        );
    }

    public async Task<List<BE_Auditoria>> BuscarAuditoria(
        BE_Auditoria filtro)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@FechaInicio", Convert.ToDateTime(filtro.FECHA_INICIO).ToString("yyyy-MM-dd")),
            _sql.CreateParameter("@FechaFin", Convert.ToDateTime(filtro.FECHA_FIN).ToString("yyyy-MM-dd")),

            _sql.CreateParameter("@Usuario", filtro.USUARIO),

            _sql.CreateParameter("@TipoAccion", filtro.TIPO_ACCION),

            _sql.CreateParameter("@Controller", filtro.CONTROLLER),

            _sql.CreateParameter("@Action", filtro.ACTION),

            _sql.CreateParameter("@BuscarTexto", filtro.BUSCAR_TEXTO),

            _sql.CreateParameter("@Origen", filtro.ORIGEN ?? (object)DBNull.Value)
        };

        return await _sql.QueryListAsync(
            "SP_BUSCAR_SEG_AUDITORIA",
            MapBuscarAuditoria,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters
        );
    }

    public async Task<BE_Auditoria?> ObtenerAuditoria(
        long idAuditoria)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@IdAuditoria", idAuditoria)
        };

        return await _sql.QuerySingleOrDefaultAsync(
            "SP_OBTENER_SEG_AUDITORIA",
            MapObtenerAuditoria,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters
        );
    }

    private static BE_RespuestaBD MapRespuestaBD(
        DbDataReader reader)
    {
        return new BE_RespuestaBD
        {
            Codigo = Convert.ToInt32(reader["Codigo"]),
            Mensaje = reader["Mensaje"]?.ToString(),
            IdGenerado = Convert.ToInt64(reader["IdGenerado"]),
            FilasAfectadas = Convert.ToInt32(reader["FilasAfectadas"])
        };
    }

    private static BE_Auditoria MapBuscarAuditoria(
        DbDataReader reader)
    {
        return new BE_Auditoria
        {
            ID_AUDITORIA =
                Convert.ToInt64(reader["IdAuditoria"]),

            FECHA_HORA =
                Convert.ToDateTime(reader["FechaHora"]),

            USUARIO =
                reader["Usuario"]?.ToString() ?? string.Empty,

            USUARIO_ID =
                reader["UsuarioId"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["UsuarioId"]),

            IP =
                reader["Ip"]?.ToString(),

            CONTROLLER =
                reader["Controller"]?.ToString(),

            ACTION =
                reader["Action"]?.ToString(),

            METODO_HTTP =
                reader["MetodoHttp"]?.ToString(),

            TIPO_ACCION =
                reader["TipoAccion"]?.ToString(),

            CODIGO_RESPUESTA =
                reader["CodigoRespuesta"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["CodigoRespuesta"]),

            TIEMPO_MS =
                reader["TiempoMs"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["TiempoMs"]),

            TIENE_EXCEPCION =
                reader["TieneExcepcion"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["TieneExcepcion"]),

            ORIGEN =
                reader["Origen"]?.ToString(),

            LATITUD =
                reader["Latitud"] == DBNull.Value
                    ? null
                    : Convert.ToDecimal(reader["Latitud"]),

            LONGITUD =
                reader["Longitud"] == DBNull.Value
                    ? null
                    : Convert.ToDecimal(reader["Longitud"])
        };
    }

    private static BE_Auditoria MapObtenerAuditoria(
        DbDataReader reader)
    {
        return new BE_Auditoria
        {
            ID_AUDITORIA =
                Convert.ToInt64(reader["IdAuditoria"]),

            FECHA_HORA =
                Convert.ToDateTime(reader["FechaHora"]),

            USUARIO =
                reader["Usuario"]?.ToString() ?? string.Empty,

            USUARIO_ID =
                reader["UsuarioId"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["UsuarioId"]),

            IP =
                reader["Ip"]?.ToString(),

            USER_AGENT =
                reader["UserAgent"]?.ToString(),

            SISTEMA_OPERATIVO =
                reader["SistemaOperativo"]?.ToString(),

            DISPOSITIVO =
                reader["Dispositivo"]?.ToString(),

            NAVEGADOR =
                reader["Navegador"]?.ToString(),

            CONTROLLER =
                reader["Controller"]?.ToString(),

            ACTION =
                reader["Action"]?.ToString(),

            URL =
                reader["Url"]?.ToString(),

            QUERY_STRING =
                reader["QueryString"]?.ToString(),

            METODO_HTTP =
                reader["MetodoHttp"]?.ToString(),

            TIPO_ACCION =
                reader["TipoAccion"]?.ToString(),

            CODIGO_RESPUESTA =
                reader["CodigoRespuesta"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["CodigoRespuesta"]),

            TIEMPO_MS =
                reader["TiempoMs"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["TiempoMs"]),

            VISTA =
                reader["Vista"]?.ToString(),

            NOMBRE_ARCHIVO =
                reader["NombreArchivo"]?.ToString(),

            PARAMETROS =
                reader["Parametros"]?.ToString(),

            EXCEPCION =
                reader["Excepcion"]?.ToString(),

            ORIGEN =
                reader["Origen"]?.ToString(),

            LATITUD =
                reader["Latitud"] == DBNull.Value
                    ? null
                    : Convert.ToDecimal(reader["Latitud"]),

            LONGITUD =
                reader["Longitud"] == DBNull.Value
                    ? null
                    : Convert.ToDecimal(reader["Longitud"])
        };
    }
}
