using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class MultiMachine {
	[JsonPropertyName("Name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("ShowPpm")]
	public bool? ShowPpm { get; set; }

	[JsonPropertyName("AutoRound")]
	public bool? AutoRound { get; set; }

	[JsonPropertyName("DefaultMax")]
	public string? DefaultMax { get; set; }

	[JsonPropertyName("Machines")]
	public List<MachineVariant>? Machines { get; set; }

	[JsonPropertyName("Capacities")]
	public List<Capacity> Capacities { get; set; } = new();
}
