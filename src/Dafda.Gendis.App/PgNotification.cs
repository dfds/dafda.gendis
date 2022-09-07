using System.Text.Json.Serialization;

namespace Dafda.Gendis.App;

public class PgNotification
{
    [JsonPropertyName("record")]
    public OutboxEntry Record { get; set; } = null!;
}