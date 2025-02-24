

using System.Text.Json.Serialization;

namespace Camunda_TZ.Models;

public class VariableModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("isValueTruncated")]
    public bool? IsValueTruncated { get; set; }

    [JsonPropertyName("previewValue")]
    public string PreviewValue { get; set; }

    [JsonPropertyName("draft")]
    public DraftModel Draft { get; set; }
}