using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

public class GameService {
	private const string DefaultGameName = "Game 1";

	private readonly SFTrackerDbContext m_context;

	public GameService(SFTrackerDbContext context) {
		m_context = context;
	}

	public async Task<List<Game>> GetAllGamesAsync() {
		return await m_context.Games
			.OrderBy(g => g.Name)
			.ToListAsync();
	}

	/// <summary>
	/// The game that was selected most recently in the UI; null only when there are no games at all.
	/// </summary>
	public async Task<Game?> GetMostRecentGameAsync() {
		return await m_context.Games
			.OrderByDescending(g => g.LastOpenedAt)
			.ThenBy(g => g.Id)
			.FirstOrDefaultAsync();
	}

	/// <summary>
	/// Startup guard: a fresh database gets one game so the UI always has something to select.
	/// </summary>
	public async Task EnsureDefaultGameAsync() {
		if (await m_context.Games.AnyAsync()) {
			return;
		}
		await CreateGameAsync(DefaultGameName);
	}

	public async Task<Game> CreateGameAsync(string name) {
		var game = new Game { Name = name.Trim() };
		m_context.Games.Add(game);
		await m_context.SaveChangesAsync();
		return game;
	}

	public async Task RenameGameAsync(int id, string name) {
		var game = await m_context.Games.FindAsync(id);
		if (game != null) {
			game.Name = name.Trim();
			game.UpdatedAt = DateTime.UtcNow;
			await m_context.SaveChangesAsync();
		}
	}

	public async Task TouchGameAsync(int id) {
		var game = await m_context.Games.FindAsync(id);
		if (game != null) {
			game.LastOpenedAt = DateTime.UtcNow;
			await m_context.SaveChangesAsync();
		}
	}

	/// <summary>
	/// Deletes a game and everything in it. Factories are deleted explicitly rather than relying on
	/// cascade, because the live DB's GameId column was added with ALTER TABLE and has no FK constraint.
	/// Recipes/inputs/outputs cascade from Factories at the DB level.
	/// </summary>
	public async Task DeleteGameAsync(int id) {
		await m_context.Factories.Where(f => f.GameId == id).ExecuteDeleteAsync();
		await m_context.Networks.Where(n => n.GameId == id).ExecuteDeleteAsync();
		await m_context.Games.Where(g => g.Id == id).ExecuteDeleteAsync();
	}
}
