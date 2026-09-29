using DA.Configuracion;
using System.Data;
using System.Data.Common;

namespace DA.AccesoDatos;

public interface ISqlExecutor
{
    DbParameter CreateParameter(
        string name,
        object? value,
        ParameterDirection direction = ParameterDirection.Input,
        int? size = null);

    Task<List<T>> QueryListAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters)
        where T : class;

    Task<T> QuerySingleAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters);

    Task<int> ExecuteNonQueryAsync(
        string commandText,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters);

    Task<DataTable> QueryDataTableAsync(
        string commandText,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters);
}