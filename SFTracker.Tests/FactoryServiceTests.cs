using Microsoft.EntityFrameworkCore;

namespace SFTracker.Tests;

public class FactoryServiceTests {
	private TestDatabase m_db = null!;
	private Data.SFTrackerDbContext m_context = null!;
	private FactoryService m_service = null!;
	private Game m_game = null!;

	[SetUp]
	public async Task SetUp() {
		m_db = await TestDatabase.CreateAsync();
		m_context = m_db.CreateContext();
		m_service = new FactoryService(m_context);
		m_game = await new GameService(m_context).CreateGameAsync("Test");
	}

	[TearDown]
	public async Task TearDown() {
		await m_context.DisposeAsync();
		m_db.Dispose();
	}

	private Task<DbRecipe> RecipeAsync(string name) =>
		m_context.Recipes.Include(r => r.Inputs).Include(r => r.Outputs).SingleAsync(r => r.Name == name);

	private Task<DbPart> PartAsync(string name) => m_context.Parts.SingleAsync(p => p.Name == name);

	[Test]
	public async Task New_factories_are_appended_to_the_manual_order() {
		var a = await m_service.CreateFactoryAsync("A", m_game.Id);
		var b = await m_service.CreateFactoryAsync("B", m_game.Id);
		var c = await m_service.CreateFactoryAsync(" C ", m_game.Id);

		var factories = await m_service.GetAllFactoriesAsync(m_game.Id);

		Assert.Multiple(() => {
			Assert.That(factories.Select(f => f.Name), Is.EqualTo(new[] { "A", "B", "C" }));
			Assert.That(new[] { a.Sorting, b.Sorting, c.Sorting }, Is.EqualTo(new[] { 0.0, 1.0, 2.0 }));
		});
	}

	[Test]
	public async Task Reordering_factories_rewrites_sorting_from_list_position() {
		var a = await m_service.CreateFactoryAsync("A", m_game.Id);
		var b = await m_service.CreateFactoryAsync("B", m_game.Id);
		var c = await m_service.CreateFactoryAsync("C", m_game.Id);

		await m_service.ReorderFactoriesAsync([c.Id, a.Id, b.Id]);

		var factories = await m_service.GetAllFactoriesAsync(m_game.Id);
		Assert.That(factories.Select(f => f.Name), Is.EqualTo(new[] { "C", "A", "B" }));
	}

	[Test]
	public async Task Factories_are_scoped_to_their_game_and_loaded_with_details() {
		var otherGame = await new GameService(m_context).CreateGameAsync("Other");
		var factory = await m_service.CreateFactoryAsync("Mine", m_game.Id);
		await m_service.CreateFactoryAsync("Theirs", otherGame.Id);
		var recipe = await RecipeAsync("Iron Ingot");
		await m_service.AddFactoryRecipeAsync(factory.Id, recipe.Id, 2);
		await m_service.AddFactoryInputAsync(factory.Id, (await PartAsync("Iron Ore")).Id, 10);

		// A fresh context, so nothing is served from the change tracker.
		await using var freshContext = m_db.CreateContext();
		var factories = await new FactoryService(freshContext).GetAllFactoriesAsync(m_game.Id);

		var loaded = factories.Single();
		Assert.Multiple(() => {
			Assert.That(loaded.Name, Is.EqualTo("Mine"));
			Assert.That(loaded.Inputs.Single().Part.Name, Is.EqualTo("Iron Ore"));
			Assert.That(loaded.Recipes.Single().Recipe.Inputs.Single().Part.Name, Is.EqualTo("Iron Ore"));
			Assert.That(loaded.Recipes.Single().Recipe.Outputs.Single().Part.Name, Is.EqualTo("Iron Ingot"));
		});
	}

	[Test]
	public async Task World_parts_toggle_per_factory_and_are_loaded_with_details() {
		var factory = await m_service.CreateFactoryAsync("Mine", m_game.Id);
		var ore = await PartAsync("Iron Ore");

		await m_service.ToggleWorldPartAsync(factory.Id, ore.Id);
		// A fresh context, so nothing is served from (or later fixed up by) the change tracker.
		await using (var freshContext = m_db.CreateContext()) {
			var marked = (await new FactoryService(freshContext).GetAllFactoriesAsync(m_game.Id)).Single();
			Assert.Multiple(() => {
				Assert.That(marked.IsWorldPart(ore.Id), Is.True);
				Assert.That(marked.WorldParts.Single().Part.Name, Is.EqualTo("Iron Ore"));
			});
		}

		await m_service.ToggleWorldPartAsync(factory.Id, ore.Id);
		var unmarked = (await m_service.GetAllFactoriesAsync(m_game.Id)).Single();
		Assert.That(unmarked.WorldParts, Is.Empty);
	}

	[Test]
	public async Task Deleting_a_factory_removes_its_world_parts() {
		var factory = await m_service.CreateFactoryAsync("Mine", m_game.Id);
		await m_service.ToggleWorldPartAsync(factory.Id, (await PartAsync("Iron Ore")).Id);

		await m_service.DeleteFactoryAsync(factory.Id);

		Assert.That(await m_context.FactoryWorldParts.CountAsync(), Is.Zero);
	}

	[Test]
	public async Task Update_sets_name_and_network() {
		var factory = await m_service.CreateFactoryAsync("Old", m_game.Id);
		var network = await new NetworkService(m_context).CreateNetworkAsync("Copper", m_game.Id);

		await m_service.UpdateFactoryAsync(factory.Id, " New ", network.Id);
		var updated = (await m_service.GetFactoryByIdAsync(factory.Id))!;
		Assert.Multiple(() => {
			Assert.That(updated.Name, Is.EqualTo("New"));
			Assert.That(updated.Network?.Name, Is.EqualTo("Copper"));
		});

		await m_service.UpdateFactoryAsync(factory.Id, "New", null);
		Assert.That((await m_service.GetFactoryByIdAsync(factory.Id))!.NetworkId, Is.Null);
	}

	[Test]
	public async Task New_recipes_are_appended_and_can_be_reordered() {
		var factory = await m_service.CreateFactoryAsync("F", m_game.Id);
		var ingot = await RecipeAsync("Iron Ingot");
		var plate = await RecipeAsync("Iron Plate");
		var first = await m_service.AddFactoryRecipeAsync(factory.Id, ingot.Id, 1);
		var second = await m_service.AddFactoryRecipeAsync(factory.Id, plate.Id, 1);
		Assert.That(second.Sorting, Is.GreaterThan(first.Sorting));

		await m_service.ReorderFactoryRecipesAsync([second.Id, first.Id]);

		var ordered = (await m_service.GetFactoryByIdAsync(factory.Id))!.Recipes.OrderBy(r => r.Sorting).Select(r => r.Id);
		Assert.That(ordered, Is.EqualTo(new[] { second.Id, first.Id }));
	}

	[Test]
	public async Task Toggling_an_input_creates_a_disabled_override_and_then_flips_it() {
		var factory = await m_service.CreateFactoryAsync("F", m_game.Id);
		var ingot = await RecipeAsync("Iron Ingot");
		var factoryRecipe = await m_service.AddFactoryRecipeAsync(factory.Id, ingot.Id, 1);
		var inputId = ingot.Inputs.Single().Id;

		await m_service.ToggleRecipeInputAsync(factoryRecipe.Id, inputId);
		Assert.That((await m_service.GetFactoryByIdAsync(factory.Id))!.Recipes.Single().IsInputEnabled(inputId), Is.False);

		await m_service.ToggleRecipeInputAsync(factoryRecipe.Id, inputId);
		Assert.Multiple(async () => {
			Assert.That((await m_service.GetFactoryByIdAsync(factory.Id))!.Recipes.Single().IsInputEnabled(inputId), Is.True);
			Assert.That(await m_context.FactoryRecipeInputOverrides.CountAsync(), Is.EqualTo(1), "the override is flipped, not duplicated");
		});
	}

	[Test]
	public async Task Switching_recipe_drops_overrides_of_the_old_recipe() {
		var factory = await m_service.CreateFactoryAsync("F", m_game.Id);
		var ingot = await RecipeAsync("Iron Ingot");
		var plate = await RecipeAsync("Iron Plate");
		var factoryRecipe = await m_service.AddFactoryRecipeAsync(factory.Id, ingot.Id, 1);
		await m_service.ToggleRecipeInputAsync(factoryRecipe.Id, ingot.Inputs.Single().Id);
		await m_service.ToggleRecipeOutputAsync(factoryRecipe.Id, ingot.Outputs.Single().Id);

		await m_service.UpdateFactoryRecipeAsync(factoryRecipe.Id, 3, isSlooped: true, recipeId: plate.Id);

		var updated = await m_context.FactoryRecipes.AsNoTracking().SingleAsync(r => r.Id == factoryRecipe.Id);
		Assert.Multiple(async () => {
			Assert.That(updated.RecipeId, Is.EqualTo(plate.Id));
			Assert.That(updated.Multiplier, Is.EqualTo(3));
			Assert.That(updated.IsSlooped, Is.True);
			Assert.That(await m_context.FactoryRecipeInputOverrides.CountAsync(), Is.Zero);
			Assert.That(await m_context.FactoryRecipeOutputOverrides.CountAsync(), Is.Zero);
		});
	}

	[Test]
	public async Task Updating_without_a_recipe_keeps_overrides() {
		var factory = await m_service.CreateFactoryAsync("F", m_game.Id);
		var ingot = await RecipeAsync("Iron Ingot");
		var factoryRecipe = await m_service.AddFactoryRecipeAsync(factory.Id, ingot.Id, 1);
		await m_service.ToggleRecipeInputAsync(factoryRecipe.Id, ingot.Inputs.Single().Id);

		await m_service.UpdateFactoryRecipeAsync(factoryRecipe.Id, 2, isSlooped: false);

		Assert.That(await m_context.FactoryRecipeInputOverrides.CountAsync(), Is.EqualTo(1));
	}

	[Test]
	public async Task Moving_a_recipe_appends_it_to_the_target_factory_and_keeps_overrides() {
		var source = await m_service.CreateFactoryAsync("Source", m_game.Id);
		var target = await m_service.CreateFactoryAsync("Target", m_game.Id);
		var ingot = await RecipeAsync("Iron Ingot");
		var plate = await RecipeAsync("Iron Plate");
		await m_service.AddFactoryRecipeAsync(target.Id, plate.Id, 1);
		var moved = await m_service.AddFactoryRecipeAsync(source.Id, ingot.Id, 2);
		await m_service.ToggleRecipeInputAsync(moved.Id, ingot.Inputs.Single().Id);

		await m_service.MoveFactoryRecipeAsync(moved.Id, target.Id);

		var sourceAfter = (await m_service.GetFactoryByIdAsync(source.Id))!;
		var targetAfter = (await m_service.GetFactoryByIdAsync(target.Id))!;
		Assert.Multiple(() => {
			Assert.That(sourceAfter.Recipes, Is.Empty);
			Assert.That(targetAfter.Recipes.OrderBy(r => r.Sorting).Select(r => r.Id), Is.EqualTo(new[] { targetAfter.Recipes.Single(r => r.RecipeId == plate.Id).Id, moved.Id }));
			Assert.That(targetAfter.Recipes.Single(r => r.Id == moved.Id).Multiplier, Is.EqualTo(2));
			Assert.That(targetAfter.Recipes.Single(r => r.Id == moved.Id).IsInputEnabled(ingot.Inputs.Single().Id), Is.False);
		});
	}

	[Test]
	public async Task Deleting_a_factory_cascades_to_its_contents() {
		var factory = await m_service.CreateFactoryAsync("F", m_game.Id);
		var ingot = await RecipeAsync("Iron Ingot");
		var factoryRecipe = await m_service.AddFactoryRecipeAsync(factory.Id, ingot.Id, 1);
		await m_service.ToggleRecipeInputAsync(factoryRecipe.Id, ingot.Inputs.Single().Id);
		await m_service.AddFactoryInputAsync(factory.Id, (await PartAsync("Iron Ore")).Id, 10);
		await m_service.AddFactoryOutputAsync(factory.Id, (await PartAsync("Iron Ingot")).Id, 10);

		await m_service.DeleteFactoryAsync(factory.Id);

		await using var freshContext = m_db.CreateContext();
		Assert.Multiple(async () => {
			Assert.That(await freshContext.Factories.CountAsync(), Is.Zero);
			Assert.That(await freshContext.FactoryRecipes.CountAsync(), Is.Zero);
			Assert.That(await freshContext.FactoryRecipeInputOverrides.CountAsync(), Is.Zero);
			Assert.That(await freshContext.FactoryInputs.CountAsync(), Is.Zero);
			Assert.That(await freshContext.FactoryOutputs.CountAsync(), Is.Zero);
		});
	}
}
