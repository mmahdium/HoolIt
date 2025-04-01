using System.Text.Json.Serialization;

namespace HoolIt.Models;

public class AddDweetSucceededResponse
{
    [JsonPropertyName("this")] public string This { get; set; }

    [JsonPropertyName("by")] public string By { get; set; }

    [JsonPropertyName("the")] public string The { get; set; }

    [JsonPropertyName("with")] public Dweet With { get; set; }
}