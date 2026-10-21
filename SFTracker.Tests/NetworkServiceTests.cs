using Microsoft.EntityFrameworkCore;

namespace SFTracker.Tests;

public class NetworkServiceTests {
	private TestDatabase m_db = null!;
	private Data.SFTrackerDbContext m_context = null!;
	private NetworkService m_service = null!;
	private FactoryService m_factories = null!;
	private Game m_game = null!;

	[SetUp]
	public async Task SetUp() {
		m_db = await TestDatabase.CreateAsync();
		m_context = m_db.CreateContext();
		m_service = new NetworkService(m_context);
		m_factories = new FactoryService(m_context);
		m_game = await new GameService(m_context).CreateGameAsync("Test");
	}

	[TearDown]
	public async Task TearDown() {
		await m_context.DisposeAsync();
		m_db.Dispose();
	}

	[Test]
	public async Task Networks_are_listed_per_game_by_name() {
		var otherGame = await new GameService(m_context).CreateGameAsync("Other");
		await m_service.CreateNetworkAsync("Steel", m_game.Id);
		await m_service.CreateNetworkAsync(" Copper ", m_game.Id);
		await m_service.CreateNetworkAsync("Elsewhere", otherGame.Id);

		var networks = await m_service.GetAllNetworksAsync(m_game.Id);

		Assert.That(networks.Select(n => n.Name), Is.EqualTo(new[] { "Copper", "Steel" }));
	}

	[Test]
	public async Task Renaming_trims_and_persists() {
		var network = await m_service.CreateNetworkAsync("Copper", m_game.Id);

		await m_service.RenameNetworkAsync(network.Id, "  Copper line ");

		var renamed = await m_context.Networks.AsNoTracking().SingleAsync(n => n.Id == network.Id);
		Assert.That(renamed.Name, Is.EqualTo("Copper line"));
	}

	[Test]
	public async Task Deleting_a_network_keeps_its_factories_unassigned() {
		var network = await m_service.CreateNetworkAsync("Copper", m_game.Id);
		var member = await m_factories.CreateFactoryAsync("Smelting", m_game.Id);
		await m_factories.UpdateFactoryAsync(member.Id, "Smelting", network.Id);

		await m_service.DeleteNetworkAsync(network.Id);

		Assert.Multiple(async () => {
			Assert.That(await m_context.Networks.CountAsync(), Is.Zero);
			Assert.That((await m_factories.GetFactoryByIdAsync(member.Id))!.NetworkId, Is.Null);
		});
	}
}
