using System.Data;
using System.Data.Common;

namespace DA.AccesoDatos;

public interface IHanaExecutor
{
    string CompanyDB { get; }

    string BuildProcedureCall(string procedureName, int parameterCount);

    DbParameter CreateParameter(
        string name,
        object? value,
        ParameterDirection direction = ParameterDirection.Input,
        int? size = null);

    Task<List<T>> QueryListAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters)
        where T : class;

    Task<T> QuerySingleAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters);

    Task<DataTable> QueryDataTableAsync(
        string commandText,
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters);
}
