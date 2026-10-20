using Microsoft.EntityFrameworkCore;

namespace SFTracker.Tests;

public class GameServiceTests {
	private TestDatabase m_db = null!;
	private Data.SFTrackerDbContext m_context = null!;
	private GameService m_service = null!;

	[SetUp]
	public async Task SetUp() {
		m_db = await TestDatabase.CreateAsync(seedGameData: false);
		m_context = m_db.CreateContext();
		m_service = new GameService(m_context);
	}

	[TearDown]
	public async Task TearDown() {
		await m_context.DisposeAsync();
		m_db.Dispose();
	}

	[Test]
	public async Task A_default_game_is_created_only_when_there_are_none() {
		await m_service.EnsureDefaultGameAsync();
		await m_service.EnsureDefaultGameAsync();
		Assert.That((await m_service.GetAllGamesAsync()).Select(g => g.Name), Is.EqualTo(new[] { "Game 1" }));

		await m_service.CreateGameAsync("Mine");
		await m_context.Games.Where(g => g.Name == "Game 1").ExecuteDeleteAsync();
		await m_service.EnsureDefaultGameAsync();
		Assert.That((await m_service.GetAllGamesAsync()).Select(g => g.Name), Is.EqualTo(new[] { "Mine" }));
	}

	[Test]
	public async Task The_most_recently_opened_game_wins() {
		var first = await m_service.CreateGameAsync("First");
		var second = await m_service.CreateGameAsync("Second");
		Assert.That((await m_service.GetMostRecentGameAsync())!.Id, Is.EqualTo(second.Id));

		await Task.Delay(10); // LastOpenedAt resolution
		await m_service.TouchGameAsync(first.Id);
		Assert.That((await m_service.GetMostRecentGameAsync())!.Id, Is.EqualTo(first.Id));
	}

	[Test]
	public void Fluid_divisor_must_be_one_of_the_allowed_values() {
		Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => m_service.SetFluidDivisorAsync(1, 4));
	}

	[Test]
	public async Task Deleting_a_game_removes_its_factories_and_networks_but_nothing_else() {
		var factories = new FactoryService(m_context);
		var networks = new NetworkService(m_context);
		var doomed = await m_service.CreateGameAsync("Doomed");
		var kept = await m_service.CreateGameAsync("Kept");
		var doomedNetwork = await networks.CreateNetworkAsync("N", doomed.Id);
		var doomedFactory = await factories.CreateFactoryAsync("F", doomed.Id);
		await factories.UpdateFactoryAsync(doomedFactory.Id, "F", doomedNetwork.Id);
		await networks.CreateNetworkAsync("N", kept.Id);
		await factories.CreateFactoryAsync("F", kept.Id);

		await m_service.DeleteGameAsync(doomed.Id);

		await using var fresh = m_db.CreateContext();
		Assert.Multiple(async () => {
			Assert.That(await fresh.Games.Select(g => g.Name).ToListAsync(), Is.EqualTo(new[] { "Kept" }));
			Assert.That(await fresh.Factories.CountAsync(), Is.EqualTo(1));
			Assert.That(await fresh.Networks.CountAsync(), Is.EqualTo(1));
			Assert.That(await fresh.Factories.SingleAsync(), Has.Property("GameId").EqualTo(kept.Id));
		});
	}
}
