using System.Text.Json;
using SFTracker.Services.Data;

namespace SFTracker.Services;

/// <summary>
/// Holds the read-only game reference data from game_data.json (singleton). The database seed reads
/// parts/recipes from it; the machine data is used for generator power output.
/// </summary>
public class GameInfoService {
	private static readonly JsonSerializerOptions s_jsonOptions = new() {
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true
	};

	private GameData? m_gameData;

	public GameData GameData =>
		m_gameData ?? throw new InvalidOperationException("Game data not loaded. Call InitializeAsync() first.");

	/// <summary>
	/// Power output in MW of one machine at 100% clock, or null when the machine is not a power
	/// generator (consumers have a negative "AveragePower" in the game data; zero means no power).
	/// </summary>
	public double? GetGeneratorPowerMW(string machineName) {
		var machine = GameData.Machines.FirstOrDefault(m => m.Name == machineName);
		if (machine?.AveragePower == null) {
			return null;
		}
		var power = DataImportService.ParseNumber(machine.AveragePower);
		return power > 0 ? power : null;
	}

	/// <summary>Loads the game data file. It is required, so a missing or broken file fails startup.</summary>
	public async Task InitializeAsync(string path) {
		await using var stream = File.OpenRead(path);
		m_gameData = await JsonSerializer.DeserializeAsync<GameData>(stream, s_jsonOptions)
			?? throw new InvalidDataException($"{path} contains no game data.");
	}
}
