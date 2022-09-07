namespace Dafda.Gendis.App;

public class OutboxEntry
{
    public string Id { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime OccuredUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
}