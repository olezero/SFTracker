using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

/// <summary>
/// CRUD for networks: named groups of factories within a game, used to balance several factories as one unit.
/// </summary>
public class NetworkService {
	private readonly SFTrackerDbContext m_context;

	public NetworkService(SFTrackerDbContext context) {
		m_context = context;
	}

	public async Task<List<Network>> GetAllNetworksAsync(int gameId) {
		return await m_context.Networks
			.Where(n => n.GameId == gameId)
			.OrderBy(n => n.Name)
			.ToListAsync();
	}

	public async Task<Network> CreateNetworkAsync(string name, int gameId) {
		var network = new Network { Name = name.Trim(), GameId = gameId };
		m_context.Networks.Add(network);
		await m_context.SaveChangesAsync();
		return network;
	}

	public async Task RenameNetworkAsync(int id, string name) {
		var network = await m_context.Networks.FindAsync(id);
		if (network != null) {
			network.Name = name.Trim();
			network.UpdatedAt = DateTime.UtcNow;
			await m_context.SaveChangesAsync();
		}
	}

	/// <summary>
	/// Deletes a network. Member factories are kept and simply become un-networked
	/// (Factories.NetworkId has a real FK, so they must be detached first).
	/// </summary>
	public async Task DeleteNetworkAsync(int id) {
		var members = await m_context.Factories.Where(f => f.NetworkId == id).ToListAsync();
		foreach (var factory in members) {
			factory.Network = null;
			factory.NetworkId = null;
		}
		var network = await m_context.Networks.FindAsync(id);
		if (network != null) {
			m_context.Networks.Remove(network);
		}
		await m_context.SaveChangesAsync();
	}
}
