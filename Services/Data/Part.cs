using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class Part : IJsonOnDeserialized {
	[JsonPropertyName("Name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("Tier")]
	public string? Tier { get; set; }

	[JsonPropertyName("SinkPoints")]
	public int SinkPoints { get; set; }

	[JsonPropertyName("ImageUrl")]
	public string? ImageUrl { get; set; }

	public void OnDeserialized() {
		// Resolve ImageUrl - this is the name with spaces replaced by underscores, and .png appended
		if (string.IsNullOrWhiteSpace(ImageUrl)) {
			ImageUrl = "images/" + Name.Replace(' ', '_') + ".png";

		}
	}
}
