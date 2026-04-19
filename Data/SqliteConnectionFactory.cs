using System.Data;
using Microsoft.Data.Sqlite;

namespace SheetsSearchApp.Data;

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Sqlite")
            ?? "Data Source=data.db;Cache=Shared;";
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
