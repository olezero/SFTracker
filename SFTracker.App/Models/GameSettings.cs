using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFTracker.Models;

/// <summary>
/// Per-game mod/tweak settings, stored as one JSON document in Games.Settings so new settings never
/// need a schema change. Everything here is applied at display/calculation time; stored factory data
/// is always in vanilla (1x machine) terms. Unknown keys survive a load/save round trip.
/// </summary>
public class GameSettings {
	private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) {
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Speed factor per machine name (as on <see cref="DbRecipe.Machine"/>), e.g. "Constructor" → 3 for a
	/// mod's Mk2 constructor that runs 3x faster. Missing = 1. The factory recipe multiplier stays in
	/// 1x machine-equivalents; this only changes how many real machines that means.
	/// </summary>
	public Dictionary<string, double> MachineSpeeds { get; set; } = [];

	[JsonExtensionData]
	public Dictionary<string, JsonElement>? Extra { get; set; }

	public double SpeedOf(string machine) =>
		MachineSpeeds.TryGetValue(machine, out var speed) && speed > 0 ? speed : 1;

	/// <summary>Parses the stored JSON; empty or invalid JSON gives default settings.</summary>
	public static GameSettings Parse(string? json) {
		if (string.IsNullOrWhiteSpace(json)) {
			return new GameSettings();
		}
		try {
			return JsonSerializer.Deserialize<GameSettings>(json, s_jsonOptions) ?? new GameSettings();
		} catch (JsonException) {
			return new GameSettings();
		}
	}

	public string ToJson() => JsonSerializer.Serialize(this, s_jsonOptions);
}
