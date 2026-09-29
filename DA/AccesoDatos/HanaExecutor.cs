using DA.Configuracion;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;

namespace DA.AccesoDatos;

public class HanaExecutor : IHanaExecutor
{
    public string CompanyDB { get; }

    private readonly string _connectionString;
    private readonly DbProviderFactory _factory;
    private readonly Func<Exception, bool> _isTransientError;
    private readonly AsyncRetryPolicy _retryPolicy;

    protected virtual int CommandTimeoutSeconds => 30;

    public HanaExecutor(IOptions<ConfiguracionConexion> conexion)
    {
        _connectionString = conexion.Value.CadenaSAP_ODBC
            ?? throw new ArgumentNullException(nameof(conexion.Value.CadenaSAP_ODBC));

        CompanyDB = conexion.Value.CompanyDB?.Trim()
            ?? throw new ArgumentNullException(nameof(conexion.Value.CompanyDB));

        _factory = OdbcFactory.Instance;
        _isTransientError = EsErrorHanaOdbcTransitorio;

        _retryPolicy = Policy
            .Handle<Exception>(ex => ex is TimeoutException || _isTransientError(ex))
            .WaitAndRetryAsync(
                3,
                intento => TimeSpan.FromSeconds(Math.Pow(2, intento)),
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[HANA RETRY {retryCount}] Esperando {timeSpan.TotalSeconds}s - Error: {exception.Message}");
                });
    }

    public string BuildProcedureCall(string procedureName, int parameterCount)
    {
        if (string.IsNullOrWhiteSpace(CompanyDB))
            throw new InvalidOperationException("CompanyDB no está configurado.");

        if (string.IsNullOrWhiteSpace(procedureName))
            throw new ArgumentException("El nombre del procedimiento no puede estar vacío.", nameof(procedureName));

        if (parameterCount < 0)
            throw new ArgumentOutOfRangeException(nameof(parameterCount), "La cantidad de parámetros no puede ser negativa.");

        var parametros = string.Join(",", Enumerable.Repeat("?", parameterCount));

        return $"CALL \"{CompanyDB}\".\"{procedureName}\"({parametros})";
    }

    public DbParameter CreateParameter(
        string name,
        object? value,
        ParameterDirection direction = ParameterDirection.Input,
        int? size = null)
    {
        var parameter = _factory.CreateParameter()
            ?? throw new InvalidOperationException("No se pudo crear el parámetro del proveedor HANA.");

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
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var result = new List<T>();

            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor HANA.");

            con.ConnectionString = _connectionString;

            using var cmd = con.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

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
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters)
        where T : class
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor HANA.");

            con.ConnectionString = _connectionString;

            using var cmd = con.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return map(reader);

            return null;
        });
    }

    public async Task<T> QuerySingleAsync<T>(
        string commandText,
        Func<DbDataReader, T> map,
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor HANA.");

            con.ConnectionString = _connectionString;

            using var cmd = con.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return map(reader);

            throw new InvalidOperationException(
                $"La consulta '{commandText}' no devolvió ningún registro y se esperaba uno.");
        });
    }

    public async Task<DataTable> QueryDataTableAsync(
        string commandText,
        CommandType commandType = CommandType.Text,
        params DbParameter[] parameters)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var table = new DataTable();

            using var con = _factory.CreateConnection()
                ?? throw new InvalidOperationException("No se pudo crear la conexión del proveedor HANA.");

            con.ConnectionString = _connectionString;

            using var cmd = con.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = CommandTimeoutSeconds;

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            table.Load(reader);

            return table;
        });
    }

    private static bool EsErrorHanaOdbcTransitorio(Exception ex)
    {
        if (ex is TimeoutException)
            return true;

        if (ex is not OdbcException odbcEx)
            return false;

        foreach (OdbcError error in odbcEx.Errors)
        {
            if (error.NativeError == 131)
                return true;
        }

        return false;
    }
}