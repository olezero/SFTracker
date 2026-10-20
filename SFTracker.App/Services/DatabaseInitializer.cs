using Microsoft.EntityFrameworkCore;
using SFTracker.Data;

namespace SFTracker.Services;

/// <summary>
/// The startup database sequence (create schema if missing, seed parts/recipes, ensure a game exists).
/// Runs at boot and again whenever the user points the app at a different database file.
/// </summary>
public static class DatabaseInitializer {
	public static async Task InitializeAsync(IServiceProvider services) {
		using var scope = services.CreateScope();
		var scoped = scope.ServiceProvider;

		// SQLite creates the file but not its folder.
		var dbPath = scoped.GetRequiredService<UserSettingsService>().EffectiveDatabasePath;
		var dbDir = Path.GetDirectoryName(dbPath);
		if (!string.IsNullOrEmpty(dbDir))
			Directory.CreateDirectory(dbDir);

		var context = scoped.GetRequiredService<SFTrackerDbContext>();
		await context.Database.EnsureCreatedAsync();

		var dataImportService = scoped.GetRequiredService<DataImportService>();
		await dataImportService.ImportPartsAsync();
		await dataImportService.ImportRecipesAsync();

		var gameService = scoped.GetRequiredService<GameService>();
		await gameService.EnsureDefaultGameAsync();
	}
}
