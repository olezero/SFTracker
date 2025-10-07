
using System.Text.Json;
using System.Text.Json.Serialization;
using SFTracker.Services.Data;

namespace SFTracker.Services;

public class GameInfoService {
	private GameData? m_gameData = null!;
	public GameData GameData {
		get {
			if (m_gameData == null) {
				throw new InvalidOperationException("Game data not initialized. Call InitializeAsync() first.");
			}
			return m_gameData;
		}
	}

	internal async Task InitializeAsync() {
		try {
			var jsonString = await File.ReadAllTextAsync("game_data.json");
			var options = new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true,
				AllowTrailingCommas = true
			};
			m_gameData = JsonSerializer.Deserialize<GameData>(jsonString, options);
		} catch (Exception ex) {
			// Log the exception or handle it appropriately
			Console.WriteLine($"Error loading game data: {ex.Message}");
			m_gameData = new GameData(); // Return empty data on error
		}
	}
}
