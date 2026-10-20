using System.Text.Json;
using SFTracker.Services.Data;

namespace SFTracker.Services;

/// <summary>
/// Holds the read-only game reference data from game_data.json (singleton). The database seed and
/// nothing else reads it at the moment; the machine data is kept for future use.
/// </summary>
public class GameInfoService {
	private static readonly JsonSerializerOptions s_jsonOptions = new() {
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true
	};

	private GameData? m_gameData;

	public GameData GameData =>
		m_gameData ?? throw new InvalidOperationException("Game data not loaded. Call InitializeAsync() first.");

	/// <summary>Loads the game data file. It is required, so a missing or broken file fails startup.</summary>
	public async Task InitializeAsync(string path) {
		await using var stream = File.OpenRead(path);
		m_gameData = await JsonSerializer.DeserializeAsync<GameData>(stream, s_jsonOptions)
			?? throw new InvalidDataException($"{path} contains no game data.");
	}
}
