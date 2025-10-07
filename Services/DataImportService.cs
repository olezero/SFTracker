using SFTracker.Data;
using SFTracker.Models;
using SFTracker.Services.Data;

namespace SFTracker.Services;

public class DataImportService {
	private readonly SFTrackerDbContext m_context;
	private readonly GameInfoService m_gameInfoService;

	public DataImportService(SFTrackerDbContext context, GameInfoService gameInfoService) {
		m_context = context;
		m_gameInfoService = gameInfoService;
	}

	public async Task ImportPartsAsync() {
		// Check if parts are already imported
		if (m_context.Parts.Any()) {
			Console.WriteLine("Parts already imported, skipping...");
			return;
		}

		Console.WriteLine("Importing parts from game data...");

		var gameData = m_gameInfoService.GameData;

		foreach (var part in gameData.Parts) {
			var dbPart = new DbPart {
				Name = part.Name,
				Tier = part.Tier,
				SinkPoints = part.SinkPoints,
				ImageUrl = part.ImageUrl
			};

			m_context.Parts.Add(dbPart);
		}

		await m_context.SaveChangesAsync();
		Console.WriteLine($"Imported {gameData.Parts.Count} parts successfully.");
	}
}
