using System.Text.Json.Serialization;

namespace SiteSnap.Domain;

public sealed class PhotoCell
{
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}
