using System.Text.Json.Serialization;

namespace HoolIt.Models;

public class AddDweetFailedResponse
{
    [JsonPropertyName("this")] public string This { get; set; }

    [JsonPropertyName("with")] public string With { get; set; }

    [JsonPropertyName("because")] public string Because { get; set; }
}