

using System.Text.Json.Serialization;

namespace Camunda_TZ.Models;

public class DraftModel
{
    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("isValueTruncated")]
    public bool? IsValueTruncated { get; set; }

    [JsonPropertyName("previewValue")]
    public string PreviewValue { get; set; }
}