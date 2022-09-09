using System.Data;
using Npgsql;

namespace Dafda.Gendis.App;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateOpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        return conn;
    }
}