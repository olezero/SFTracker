using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Tests;

public class GameTransferServiceTests {
	private TestDatabase m_db = null!;
	private Data.SFTrackerDbContext m_context = null!;
	private GameTransferService m_transfer = null!;

	[SetUp]
	public async Task SetUp() {
		m_db = await TestDatabase.CreateAsync();
		m_context = m_db.CreateContext();
		m_transfer = new GameTransferService(m_context);
	}

	[TearDown]
	public async Task TearDown() {
		await m_context.DisposeAsync();
		m_db.Dispose();
	}

	/// <summary>A game that exercises every exported feature: networks, inputs/outputs, sloop, overrides, ordering.</summary>
	private async Task<Game> CreateSampleGameAsync() {
		var games = new GameService(m_context);
		var factories = new FactoryService(m_context);
		var game = await games.CreateGameAsync("Save A");
		await games.SetFluidDivisorAsync(game.Id, 2);
		var network = await new NetworkService(m_context).CreateNetworkAsync("Copper line", game.Id);

		var ingot = await m_context.Recipes.Include(r => r.Inputs).Include(r => r.Outputs).SingleAsync(r => r.Name == "Iron Ingot");
		var plate = await m_context.Recipes.SingleAsync(r => r.Name == "Iron Plate");
		var ore = await m_context.Parts.SingleAsync(p => p.Name == "Iron Ore");

		var smelting = await factories.CreateFactoryAsync("Smelting", game.Id);
		await factories.UpdateFactoryAsync(smelting.Id, "Smelting", network.Id);
		await factories.AddFactoryInputAsync(smelting.Id, ore.Id, 30);
		await factories.AddFactoryOutputAsync(smelting.Id, ingot.Outputs.Single().PartId, 10);
		var slooped = await factories.AddFactoryRecipeAsync(smelting.Id, ingot.Id, 2.5, isSlooped: true);
		await factories.ToggleRecipeInputAsync(slooped.Id, ingot.Inputs.Single().Id);
		await factories.ToggleRecipeOutputAsync(slooped.Id, ingot.Outputs.Single().Id);
		var plates = await factories.AddFactoryRecipeAsync(smelting.Id, plate.Id, 1);
		await factories.ReorderFactoryRecipesAsync([plates.Id, slooped.Id]);
		await factories.ToggleWorldPartAsync(smelting.Id, ore.Id);

		await factories.CreateFactoryAsync("Unassigned", game.Id);
		return game;
	}

	private static GameTransferService.GameExport Parse(string json) =>
		JsonSerializer.Deserialize<GameTransferService.GameExport>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

	[Test]
	public async Task Export_describes_the_game_by_names() {
		var game = await CreateSampleGameAsync();

		var export = Parse(await m_transfer.ExportAsync(game.Id));

		var smelting = export.Factories.Single(f => f.Name == "Smelting");
		Assert.Multiple(() => {
			Assert.That(export.Format, Is.EqualTo("sftracker-game"));
			Assert.That(export.Name, Is.EqualTo("Save A"));
			Assert.That(export.FluidDivisor, Is.EqualTo(2));
			Assert.That(export.Networks, Is.EqualTo(new[] { "Copper line" }));
			Assert.That(export.Factories.Select(f => f.Name), Is.EqualTo(new[] { "Smelting", "Unassigned" }));
			Assert.That(smelting.Network, Is.EqualTo("Copper line"));
			Assert.That(smelting.Inputs.Single().Part, Is.EqualTo("Iron Ore"));
			Assert.That(smelting.Inputs.Single().AmountPerMinute, Is.EqualTo(30));
			Assert.That(smelting.Recipes.Select(r => r.Recipe), Is.EqualTo(new[] { "Iron Plate", "Iron Ingot" }), "manual order is kept");
			Assert.That(smelting.Recipes[1].Multiplier, Is.EqualTo(2.5));
			Assert.That(smelting.Recipes[1].IsSlooped, Is.True);
			Assert.That(smelting.Recipes[1].DisabledInputs, Is.EqualTo(new[] { "Iron Ore" }));
			Assert.That(smelting.Recipes[1].DisabledOutputs, Is.EqualTo(new[] { "Iron Ingot" }));
			Assert.That(smelting.WorldParts, Is.EqualTo(new[] { "Iron Ore" }));
			Assert.That(export.Factories.Single(f => f.Name == "Unassigned").Network, Is.Null);
		});
	}

	[Test]
	public async Task Import_recreates_the_game_as_a_new_one_and_round_trips_exactly() {
		var game = await CreateSampleGameAsync();
		var original = await m_transfer.ExportAsync(game.Id);

		var imported = await m_transfer.ImportAsync(original);

		Assert.That(imported.Id, Is.Not.EqualTo(game.Id));
		Assert.That(imported.Name, Is.EqualTo("Save A (imported)"));

		// Re-exporting the import must yield the same document apart from name and timestamp.
		var reexported = Parse(await m_transfer.ExportAsync(imported.Id));
		var expected = Parse(original);
		reexported.Name = expected.Name;
		reexported.ExportedAt = expected.ExportedAt;
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
		Assert.That(JsonSerializer.Serialize(reexported, options), Is.EqualTo(JsonSerializer.Serialize(expected, options)));
	}

	[Test]
	public async Task Import_never_touches_existing_games_and_numbers_repeated_imports() {
		var game = await CreateSampleGameAsync();
		var json = await m_transfer.ExportAsync(game.Id);

		await m_transfer.ImportAsync(json);
		var second = await m_transfer.ImportAsync(json);

		Assert.Multiple(async () => {
			Assert.That(second.Name, Is.EqualTo("Save A (imported) (2)"));
			Assert.That(await m_context.Games.CountAsync(), Is.EqualTo(3));
			Assert.That(await m_context.Factories.CountAsync(f => f.GameId == game.Id), Is.EqualTo(2));
		});
	}

	[Test]
	public async Task Import_rejects_unknown_parts_and_recipes_before_writing_anything() {
		var game = await CreateSampleGameAsync();
		var json = (await m_transfer.ExportAsync(game.Id))
			.Replace("\"Iron Plate\"", "\"Iron Plate Deluxe\"")
			.Replace("\"part\": \"Iron Ore\"", "\"part\": \"Unobtainium\"");

		var ex = Assert.ThrowsAsync<GameImportException>(() => m_transfer.ImportAsync(json))!;

		Assert.Multiple(async () => {
			Assert.That(ex.Message, Does.Contain("recipe \"Iron Plate Deluxe\""));
			Assert.That(ex.Message, Does.Contain("part \"Unobtainium\""));
			Assert.That(await m_context.Games.CountAsync(), Is.EqualTo(1), "nothing was written");
		});
	}

	[TestCase("{ not json")]
	[TestCase("{ \"format\": \"something-else\", \"name\": \"x\" }")]
	[TestCase("{ \"format\": \"sftracker-game\", \"version\": 99, \"name\": \"x\" }")]
	[TestCase("{ \"format\": \"sftracker-game\", \"name\": \"  \" }")]
	public void Import_rejects_documents_that_are_not_a_usable_export(string json) {
		Assert.ThrowsAsync<GameImportException>(() => m_transfer.ImportAsync(json));
	}
}
