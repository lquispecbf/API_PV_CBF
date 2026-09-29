using System.Data;
using System.Data.Common;
using BE;
using BE.Auditoria;
using DA.AccesoDatos;
using DA.Configuracion;

namespace DA.Repositorio.Repositorio_Auditoria;

public class EndpointAuditoria : IEndpointAuditoria
{
    private readonly ISqlExecutor _sql;

    public EndpointAuditoria(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<BE_EndpointAuditoria?> ObtenerEndpoint(
        string controller,
        string action,
        string? origen = "INTRANET")
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@Controller", controller),
            _sql.CreateParameter("@Action", action),
            _sql.CreateParameter("@Origen", origen ?? (object)"INTRANET")
        };

        var respuesta = await _sql.QuerySingleOrDefaultAsync(
            "SP_SEG_ENDPOINT_AUDITORIA_OBTENER",
            MapEndpointAuditoria,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters);
        return respuesta;
    }

    public async Task<List<BE_EndpointAuditoria>> Buscar_EndpointAuditoria(
        BE_EndpointAuditoria filtro)
    {
        if (filtro is null)
            filtro = new BE_EndpointAuditoria();

        var parameters = new[]
        {
            _sql.CreateParameter("@Controller", filtro.CONTROLLER),
            _sql.CreateParameter("@Action", filtro.ACTION),
            _sql.CreateParameter("@IdTipoAccion",
                filtro.ID_TIPO_ACCION == 0 ? (object)DBNull.Value : filtro.ID_TIPO_ACCION),
            _sql.CreateParameter("@Auditar",
                filtro.AUDITAR.HasValue ? (Convert.ToBoolean(filtro.AUDITAR) ? 1 : 0) : (object)DBNull.Value),
            _sql.CreateParameter("@Activo",
                filtro.ACTIVO.HasValue ? (Convert.ToBoolean(filtro.ACTIVO) ? 1 : 0) : (object)DBNull.Value),

            _sql.CreateParameter("@Origen", filtro.ORIGEN ?? (object)DBNull.Value)
        };

        return await _sql.QueryListAsync(
            "SP_BUSCAR_SEG_ENDPOINT_AUDITORIA",
            MapEndpointAuditoria,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters);
    }

    public async Task<BE_EndpointAuditoria?> Obtener_EndpointAuditoria(
        long idEndpoint)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@IdEndpoint", idEndpoint)
        };

        return await _sql.QuerySingleOrDefaultAsync(
            "SP_OBTENER_SEG_ENDPOINT_AUDITORIA",
            MapEndpointAuditoria,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters);
    }

    public async Task<BE_RespuestaBD> Insertar_EndpointAuditoria(
        BE_EndpointAuditoria obj)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@Controller", obj.CONTROLLER),
            _sql.CreateParameter("@Action", obj.ACTION),
            _sql.CreateParameter("@IdTipoAccion", obj.ID_TIPO_ACCION),
            _sql.CreateParameter("@Auditar", obj.AUDITAR ?? false),
            _sql.CreateParameter("@Activo", obj.ACTIVO ?? true),
            _sql.CreateParameter("@Usuario", DBNull.Value),
            _sql.CreateParameter("@Origen", obj.ORIGEN ?? (object)"INTRANET")
        };

        return await _sql.QuerySingleAsync(
            "SP_INSERTAR_SEG_ENDPOINT_AUDITORIA",
            MapRespuestaBD,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters);
    }

    public async Task<BE_RespuestaBD> Actualizar_EndpointAuditoria(
        BE_EndpointAuditoria obj)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@IdEndpoint", obj.ID_ENDPOINT),
            _sql.CreateParameter("@Controller", obj.CONTROLLER),
            _sql.CreateParameter("@Action", obj.ACTION),
            _sql.CreateParameter("@IdTipoAccion", obj.ID_TIPO_ACCION),
            _sql.CreateParameter("@Auditar", obj.AUDITAR ?? false),
            _sql.CreateParameter("@Activo", obj.ACTIVO ?? true),
            _sql.CreateParameter("@Usuario", DBNull.Value),
            _sql.CreateParameter("@Origen", obj.ORIGEN ?? (object)DBNull.Value)
        };

        var respuesta = await _sql.QuerySingleAsync(
            "SP_ACTUALIZAR_SEG_ENDPOINT_AUDITORIA",
            MapRespuestaBD,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters);
        return respuesta;
    }

    public async Task<BE_RespuestaBD> Eliminar_EndpointAuditoria(
        BE_EndpointAuditoria obj)
    {
        var parameters = new[]
        {
            _sql.CreateParameter("@IdEndpoint", obj.ID_ENDPOINT),
            _sql.CreateParameter("@Usuario", DBNull.Value)
        };

        return await _sql.QuerySingleAsync(
            "SP_ELIMINAR_SEG_ENDPOINT_AUDITORIA",
            MapRespuestaBD,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure,
            parameters);
    }

    public async Task<List<BE_TipoAccion>> Obtener_Combo_TipoAccion()
    {
        return await _sql.QueryListAsync(
            "SP_OBTENER_COMBO_TIPO_ACCION",
            MapTipoAccion,
            TipoConexionSql.Produccion,
            CommandType.StoredProcedure);
    }

    private static BE_EndpointAuditoria MapEndpointAuditoria(
        DbDataReader reader)
    {
        return new BE_EndpointAuditoria
        {
            ID_ENDPOINT =
                Convert.ToInt64(reader["IdEndpoint"]),

            CONTROLLER =
                reader["Controller"]?.ToString() ?? string.Empty,

            ACTION =
                reader["Action"]?.ToString() ?? string.Empty,

            ID_TIPO_ACCION =
                Convert.ToInt32(reader["IdTipoAccion"]),

            TIPO_ACCION =
                reader["TipoAccion"]?.ToString() ?? string.Empty,

            AUDITAR =
                Convert.ToBoolean(reader["Auditar"]),

            ACTIVO =
                Convert.ToBoolean(reader["Activo"]),

            ORIGEN =
                reader["Origen"]?.ToString()
        };
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

    private static BE_TipoAccion MapTipoAccion(
        DbDataReader reader)
    {
        return new BE_TipoAccion
        {
            ID_TIPO_ACCION =
                Convert.ToInt32(reader["IdTipoAccion"]),

            CODIGO =
                reader["Codigo"]?.ToString() ?? string.Empty,

            NOMBRE =
                reader["Nombre"]?.ToString() ?? string.Empty
        };
    }
}
