using Dapper;
using Microsoft.Data.SqlClient;

namespace RepoPulse.Cli.Data;

public sealed class Db
{
    private readonly string _cs;

    public Db(string connectionString)
    {
        _cs = connectionString;
    }

    public async Task EnsureReadyAsync()
    {
        // Create DB if missing, then ensure schema exists
        var builder = new SqlConnectionStringBuilder(_cs);
        var dbName = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "RepoPulseDb" : builder.InitialCatalog;

        await DbBootstrap.EnsureDatabaseExistsAsync(_cs, dbName);
        await EnsureSchemaAsync();
    }

    public async Task<SqlConnection> OpenAsync()
    {
        var conn = new SqlConnection(_cs);
        await conn.OpenAsync();
        return conn;
    }

    private async Task EnsureSchemaAsync()
    {
        await using var conn = await OpenAsync();
        await conn.ExecuteAsync(Schema.Sql);
    }
}