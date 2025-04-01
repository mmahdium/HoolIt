using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;

namespace HoolIt.Models;

public class Dweet
{
    [JsonPropertyName("thing")] public string Thing { get; set; }
    [JsonPropertyName("created")] public DateTime Created { get; set; }
    [JsonPropertyName("content")] public Dictionary<string, string> Content { get; set; }
}