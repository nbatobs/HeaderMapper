using System.Text.Json.Serialization;

namespace HeaderMapper.Models;

public class ColumnSchema
{
    [JsonPropertyName("canonicalName")]
    public string CanonicalName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("exampleValues")]
    public List<string> ExampleValues { get; set; } = new();

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; set; } = new();
}
