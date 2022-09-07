using System.Collections.Concurrent;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace Dafda.Gendis.App;

public class Dispatcher : BackgroundService, IDisposable
{
    private static readonly string ConnectionString = "User ID=postgres;Password=p;Host=localhost;Port=5432;Database=dafdagendis;";
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly AutoResetEvent _resetEvent = new(false);

    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var thread = new Thread(new ParameterizedThreadStart(ThreadStart));
            thread.IsBackground = true;
            thread.Start(stoppingToken);

            await using var conn = new NpgsqlConnection(ConnectionString);
            conn.Notification += HandleNotification;

            await conn.OpenAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await conn.ExecuteAsync("LISTEN dafda_outbox", commandTimeout: 20);
                await conn.WaitAsync(stoppingToken);
            }

            _resetEvent.Close();
            thread.Join(TimeSpan.FromSeconds(30));

        }, stoppingToken);
    }

    private void HandleNotification(object sender, NpgsqlNotificationEventArgs args)
    {
        _queue.Enqueue(args.Payload);
        _resetEvent.Set();
    }

    private void ThreadStart(object? obj)
    {
        if (obj is not CancellationToken cancellationToken)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            _resetEvent.WaitOne(TimeSpan.FromSeconds(10));

            if (!_queue.IsEmpty)
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                ProcessQueue(conn, cancellationToken);
            }
        }
    }

    private void ProcessQueue(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        Action<string> log = (msg) => Console.WriteLine("[THREAD] " + msg);

        while (!cancellationToken.IsCancellationRequested && !_queue.IsEmpty)
        {
            if (_queue.TryPeek(out var payload))
            {
                var item = JsonSerializer.Deserialize<PgNotification>(payload);
                conn.Execute($"update \"_outbox\" set \"ProcessedUtc\" = '{DateTime.UtcNow}' where \"Id\" = '{item!.Record.Id}'");

                log($"processed {item!.Record.Id}");

                // finally
                _queue.TryDequeue(out _);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _resetEvent?.Dispose();
    }
}