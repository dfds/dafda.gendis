public class SystemTime
{
    private readonly Func<DateTime> _provider;

    public SystemTime(Func<DateTime> provider)
    {
        _provider = provider;
    }

    public DateTime UtcNow => _provider().ToUniversalTime();

    public static SystemTime CreateDefault() => new SystemTime(() => DateTime.Now);
}