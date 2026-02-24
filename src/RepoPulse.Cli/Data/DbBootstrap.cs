using Dapper;
using Microsoft.Data.SqlClient;

namespace RepoPulse.Cli.Data;

public static class DbBootstrap
{
    public static async Task EnsureDatabaseExistsAsync(string connectionString, string dbName)
    {
        // Connect to master to create the DB if needed
        var masterCs = WithDatabase(connectionString, "master");

        await using var conn = new SqlConnection(masterCs);
        await conn.OpenAsync();

        var sql = @"
IF DB_ID(@DbName) IS NULL
BEGIN
    DECLARE @q NVARCHAR(MAX) = N'CREATE DATABASE [' + REPLACE(@DbName, ']', ']]') + N']';
    EXEC (@q);
END
";
        await conn.ExecuteAsync(sql, new { DbName = dbName });
    }

    public static string WithDatabase(string connectionString, string dbName)
    {
        var b = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = dbName
        };
        return b.ConnectionString;
    }
}