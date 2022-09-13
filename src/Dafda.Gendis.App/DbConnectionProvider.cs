using System.Data;
using Npgsql;

namespace Dafda.Gendis.App;

public class DbConnectionProvider : IDbConnectionProvider
{
    private readonly NpgsqlConnection _connection;

    public DbConnectionProvider(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public IDbConnection Get() => _connection;
}