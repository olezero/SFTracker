using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class Capacity {
	[JsonPropertyName("Name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("PartsRatio")]
	public string? PartsRatio { get; set; }

	[JsonPropertyName("PowerRatio")]
	public string? PowerRatio { get; set; }

	[JsonPropertyName("Color")]
	public int? Color { get; set; }

	[JsonPropertyName("Default")]
	public bool? Default { get; set; }

	[JsonPropertyName("Description")]
	public string? Description { get; set; }
}
