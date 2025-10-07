using System.Text.Json.Serialization;

namespace SFTracker.Services.Data;

public class Machine {
	[JsonPropertyName("Name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("Tier")]
	public string? Tier { get; set; }

	[JsonPropertyName("AveragePower")]
	public string? AveragePower { get; set; }

	[JsonPropertyName("MinPower")]
	public string? MinPower { get; set; }

	[JsonPropertyName("BasePower")]
	public string? BasePower { get; set; }

	[JsonPropertyName("BasePowerBoost")]
	public string? BasePowerBoost { get; set; }

	[JsonPropertyName("FueledBasePowerBoost")]
	public string? FueledBasePowerBoost { get; set; }

	[JsonPropertyName("OverclockPowerExponent")]
	public string? OverclockPowerExponent { get; set; }

	[JsonPropertyName("MaxProductionShards")]
	public int? MaxProductionShards { get; set; }

	[JsonPropertyName("ProductionShardMultiplier")]
	public string? ProductionShardMultiplier { get; set; }

	[JsonPropertyName("ProductionShardPowerExponent")]
	public string? ProductionShardPowerExponent { get; set; }

	[JsonPropertyName("Cost")]
	public List<Cost> Cost { get; set; } = new();
}
