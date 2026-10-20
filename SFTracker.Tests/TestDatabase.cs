using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SFTracker.Data;

namespace SFTracker.Tests;

/// <summary>
/// An in-memory SQLite database with the app's schema, optionally seeded with the real game_data.json.
/// Every DbContext created from it shares the same connection, so they see the same data.
/// </summary>
internal sealed class TestDatabase : IDisposable {
	private static readonly Lazy<Task<GameInfoService>> s_gameInfo = new(LoadGameInfoAsync);
	private readonly SqliteConnection m_connection;

	private TestDatabase() {
		m_connection = new SqliteConnection("Data Source=:memory:");
		m_connection.Open();
		Options = new DbContextOptionsBuilder<SFTrackerDbContext>().UseSqlite(m_connection).Options;
	}

	public DbContextOptions<SFTrackerDbContext> Options { get; }

	public SFTrackerDbContext CreateContext() => new(Options);

	public static async Task<TestDatabase> CreateAsync(bool seedGameData = true) {
		var db = new TestDatabase();
		await using var context = db.CreateContext();
		await context.Database.EnsureCreatedAsync();
		if (seedGameData) {
			var import = new DataImportService(context, await GameInfoAsync(), NullLogger<DataImportService>.Instance);
			await import.ImportPartsAsync();
			await import.ImportRecipesAsync();
		}
		return db;
	}

	/// <summary>The app's game_data.json, loaded once per test run.</summary>
	public static Task<GameInfoService> GameInfoAsync() => s_gameInfo.Value;

	private static async Task<GameInfoService> LoadGameInfoAsync() {
		var service = new GameInfoService();
		await service.InitializeAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "game_data.json"));
		return service;
	}

	public void Dispose() => m_connection.Dispose();
}
