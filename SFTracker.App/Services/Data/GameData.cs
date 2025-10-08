using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class GameData {
	[JsonPropertyName("Machines")]
	public List<Machine> Machines { get; set; } = new();

	[JsonPropertyName("MultiMachines")]
	public List<MultiMachine> MultiMachines { get; set; } = new();

	[JsonPropertyName("Parts")]
	public List<Part> Parts { get; set; } = new();

	[JsonPropertyName("Recipes")]
	public List<Recipe> Recipes { get; set; } = new();
}
