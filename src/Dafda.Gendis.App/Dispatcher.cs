using System.Collections.Concurrent;
using Dapper;
using Npgsql;

namespace Dafda.Gendis.App;

public class Dispatcher : BackgroundService
{
    private static readonly string ConnectionString = "User ID=postgres;Password=p;Host=localhost;Port=5432;Database=dafdagendis;";
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            using var resetEvent = new AutoResetEvent(true);

            var thread = new Thread(new ParameterizedThreadStart(ThreadStart));
            thread.IsBackground = true;
            thread.Start(new ThreadData(resetEvent, _serviceProvider, stoppingToken));

            await using var conn = new NpgsqlConnection(ConnectionString);
            
            // ReSharper disable once AccessToDisposedClosure
            conn.Notification += (_, _) => resetEvent.Set();

            await conn.OpenAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await conn.ExecuteAsync("LISTEN dafda_outbox", commandTimeout: 10);
                await conn.WaitAsync(stoppingToken);
            }

            resetEvent.Close();
            thread.Join(TimeSpan.FromSeconds(30));

        }, stoppingToken);
    }

    private void ThreadStart(object? obj)
    {
        if (obj is not ThreadData(var resetEvent, var serviceProvider, var cancellationToken))
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<QueueProcessor>();
            processor.ProcessQueue(cancellationToken);

            //ProcessQueue(cancellationToken);
            resetEvent.WaitOne(TimeSpan.FromSeconds(10));
        }
    }

    private void ProcessQueue(CancellationToken cancellationToken)
    {
        Action<string> log = (msg) => Console.WriteLine("[THREAD] " + msg);

        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        var entries = conn
            .Query<OutboxEntry>("select * from \"_outbox\" where \"ProcessedUtc\" is null order by \"OccurredUtc\"")
            .ToArray();

        log($"items to process: {entries.Length}");

        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            conn.Execute($"update \"_outbox\" set \"ProcessedUtc\" = '{DateTime.UtcNow}' where \"Id\" = '{entry.Id}'");
            log($"processed: #{entry.Id} - with payload: {entry.Payload}");
        }
    }

    private class ThreadData
    {
        public ThreadData(AutoResetEvent resetEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            ResetEvent = resetEvent;
            ServiceProvider = serviceProvider;
        }

        public CancellationToken CancellationToken { get; }
        public AutoResetEvent ResetEvent { get; }
        public IServiceProvider ServiceProvider { get; }

        public void Deconstruct(out AutoResetEvent resetEvent, out IServiceProvider serviceProvider,
            out CancellationToken cancellationToken)
        {
            cancellationToken = CancellationToken;
            resetEvent = ResetEvent;
            serviceProvider = ServiceProvider;
        }
    }
}

public class ActualDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public ActualDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {

        }, stoppingToken);
    }
}
