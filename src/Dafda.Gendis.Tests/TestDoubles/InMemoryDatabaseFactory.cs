using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Dafda.Gendis.Tests.TestDoubles;

public class InMemoryDatabaseFactory : IDisposable, IAsyncDisposable
{
    private SqliteConnection? _connection;

    static InMemoryDatabaseFactory()
    {
        SqlMapper.AddTypeHandler(new GuidConverter());
        SqlMapper.AddTypeHandler(new DateTimeConverter());
    }

    public async Task<IDbConnection> CreateDbConnection()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        await _connection.ExecuteAsync(@"
            CREATE TABLE _outbox(  
               ID TEXT PRIMARY KEY NOT NULL,  
               Topic TEXT NOT NULL,  
               Key TEXT NOT NULL, 
               Payload TEXT NOT NULL, 
               OccurredUtc TEXT NOT NULL, 
               ProcessedUtc TEXT NULL
            )
        ");

        return _connection;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    #region custom value mappers for sqlite

    private class GuidConverter : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) 
            => Guid.Parse((string)value);
        
        public override void SetValue(IDbDataParameter parameter, Guid value) 
            => parameter.Value = value.ToString("N");
    }

    private class DateTimeConverter : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) 
            => DateTime.Parse((string) value);

        public override void SetValue(IDbDataParameter parameter, DateTime value) 
            => parameter.Value = value.ToString(CultureInfo.InvariantCulture);
    }


    #endregion
}
