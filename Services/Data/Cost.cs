using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class Cost {
	[JsonPropertyName("Part")]
	public string Part { get; set; } = string.Empty;

	[JsonPropertyName("Amount")]
	public string Amount { get; set; } = string.Empty;
}
