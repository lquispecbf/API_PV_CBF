using DA.Configuracion;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace DA.AccesoDatos;

public class SqlExecutor : ISqlExecutor
{
    private readonly ConfiguracionConexion _config;
    private readonly DbProviderFactory _factory;
    private readonly Func<Exception, bool> _isTransientError;
    private readonly AsyncRetryPolicy _retryPolicy;

    protected virtual int CommandTimeoutSeconds => 30;

    public SqlExecutor(IOptions<ConfiguracionConexion> conexion)
    {
        _config = conexion.Value;

        _factory = SqlClientFactory.Instance;
        _isTransientError = EsErrorSqlServerTransitorio;

        _retryPolicy = Policy
            .Handle<Exception>(ex => ex is TimeoutException || _isTransientError(ex))
            .WaitAndRetryAsync(
                3,
                intento => TimeSpan.FromSeconds(Math.Pow(2, intento)),
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[SQL RETRY {retryCount}] Esperando {timeSpan.TotalSeconds}s - Error: {exception.Message}");
                });
    }

    private string ObtenerCadenaConexion(TipoConexionSql tipoConexion)
    {
        return tipoConexion switch
        {
            TipoConexionSql.Produccion => _config.CadenaSQL,

            TipoConexionSql.POS => _config.CadenaSQLPOS,

            TipoConexionSql.Extranet => _config.CadenaSQLExt,

            _ => throw new ArgumentOutOfRangeException(nameof(tipoConexion))
        };
    }

    public DbParameter CreateParameter(
        string name,
        object? value,
        ParameterDirection direction = ParameterDirection.Input,
        int? size = null)
    {
        var parameter = _factory.CreateParameter()
            ?? throw new InvalidOperationException("No se pudo crear el parámetro del proveedor SQL Server.");

        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        parameter.Direction = direction;

        if (size.HasValue)
            parameter.Size = size.Value;

        return parameter;
    }

    public async Task<List<T>> QueryListAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var result = new List<T>();

            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor SQL Server.");

            con.ConnectionString = ObtenerCadenaConexion(tipoConexion);

            using var cmd = con.CreateCommand();

            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            AgregarParametros(cmd, parameters);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(map(reader));
            }

            return result;
        });
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters)
        where T : class
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor SQL Server.");

            con.ConnectionString = ObtenerCadenaConexion(tipoConexion);

            using var cmd = con.CreateCommand();

            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            AgregarParametros(cmd, parameters);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return map(reader);
            }

            return null;
        });
    }

    public async Task<T> QuerySingleAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor SQL Server.");

            con.ConnectionString = ObtenerCadenaConexion(tipoConexion);

            using var cmd = con.CreateCommand();

            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            AgregarParametros(cmd, parameters);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return map(reader);
            }

            throw new InvalidOperationException(
                $"La consulta '{commandText}' no devolvió ningún registro y se esperaba uno.");
        });
    }

    public async Task<int> ExecuteNonQueryAsync(
        string commandText,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor SQL Server.");

            con.ConnectionString = ObtenerCadenaConexion(tipoConexion);

            using var cmd = con.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            AgregarParametros(cmd, parameters);

            await con.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        });
    }

    public async Task<DataTable> QueryDataTableAsync(
        string commandText,
        TipoConexionSql tipoConexion = TipoConexionSql.Produccion,
        CommandType commandType = CommandType.StoredProcedure,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var table = new DataTable();

            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor SQL Server.");

            con.ConnectionString = ObtenerCadenaConexion(tipoConexion);

            using var cmd = con.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            AgregarParametros(cmd, parameters);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            table.Load(reader);

            return table;
        });
    }

    private static bool EsErrorSqlServerTransitorio(Exception ex)
    {
        if (ex is not SqlException sqlEx)
            return false;

        foreach (SqlError error in sqlEx.Errors)
        {
            switch (error.Number)
            {
                case -2:
                case 1205:
                case 4060:
                case 10928:
                case 10929:
                case 40197:
                case 40501:
                case 40613:
                    return true;
            }
        }

        return false;
    }

    private static void AgregarParametros(DbCommand cmd, DbParameter[]? parameters)
    {
        if (parameters is null)
            return;

        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(ClonarParametro(parameter));
        }
    }

    private static DbParameter ClonarParametro(DbParameter parameter)
    {
        if (parameter is ICloneable clonable)
            return (DbParameter)clonable.Clone();

        throw new InvalidOperationException(
            $"No se pudo clonar el parámetro '{parameter.ParameterName}' para ejecutar el comando SQL.");
    }
}
