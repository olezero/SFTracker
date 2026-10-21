using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace SFTracker.Tests;

public class DataImportServiceTests {
	[TestCase("30", 30)]
	[TestCase("-1", -1)]
	[TestCase("12/5", 2.4)]
	[TestCase("-9/5", -1.8)]
	[TestCase(" 2 / 3 ", 2.0 / 3)]
	public void ParseNumber_handles_plain_numbers_and_fractions(string value, double expected) {
		Assert.That(DataImportService.ParseNumber(value), Is.EqualTo(expected).Within(1e-12));
	}

	[TestCase("")]
	[TestCase("abc")]
	[TestCase("1/0")]
	[TestCase("1/2/3")]
	public void ParseNumber_rejects_garbage(string value) {
		Assert.That(() => DataImportService.ParseNumber(value), Throws.TypeOf<FormatException>());
	}

	[Test]
	public async Task Imports_every_part_and_recipe_from_game_data() {
		using var db = await TestDatabase.CreateAsync();
		var gameData = (await TestDatabase.GameInfoAsync()).GameData;
		await using var context = db.CreateContext();

		var ironIngot = await context.Recipes
			.Include(r => r.Inputs).ThenInclude(i => i.Part)
			.Include(r => r.Outputs).ThenInclude(o => o.Part)
			.SingleAsync(r => r.Name == "Iron Ingot");

		Assert.Multiple(async () => {
			Assert.That(await context.Parts.CountAsync(), Is.EqualTo(gameData.Parts.Count));
			Assert.That(await context.Recipes.CountAsync(), Is.EqualTo(gameData.Recipes.Count));
			Assert.That(ironIngot.Machine, Is.EqualTo("Smelter"));
			Assert.That(ironIngot.TimeSeconds, Is.EqualTo(2));
			Assert.That(ironIngot.Inputs.Single().Part.Name, Is.EqualTo("Iron Ore"));
			Assert.That(ironIngot.Inputs.Single().Quantity, Is.EqualTo(1), "inputs are stored as positive quantities");
			Assert.That(ironIngot.Outputs.Single().Part.Name, Is.EqualTo("Iron Ingot"));
		});
	}

	[Test]
	public async Task Fractional_batch_times_and_amounts_are_imported() {
		using var db = await TestDatabase.CreateAsync();
		await using var context = db.CreateContext();

		var nitroRocketFuel = await context.Recipes.SingleAsync(r => r.Name == "Nitro Rocket Fuel");
		var cokeGenerator = await context.Recipes.Include(r => r.Inputs).ThenInclude(i => i.Part)
			.SingleAsync(r => r.Name == "Petroleum Coke Generator");

		Assert.Multiple(() => {
			Assert.That(nitroRocketFuel.TimeSeconds, Is.EqualTo(2.4).Within(1e-12));
			Assert.That(cokeGenerator.Inputs.Single(i => i.Part.Name == "Water").Quantity, Is.EqualTo(1.8).Within(1e-12));
		});
	}

	[Test]
	public async Task Import_is_idempotent_and_only_adds_missing_entries() {
		using var db = await TestDatabase.CreateAsync();
		await using var context = db.CreateContext();
		var import = new DataImportService(context, await TestDatabase.GameInfoAsync(), NullLogger<DataImportService>.Instance);
		var partCount = await context.Parts.CountAsync();
		var recipeCount = await context.Recipes.CountAsync();

		await import.ImportPartsAsync();
		await import.ImportRecipesAsync();
		Assert.That(await context.Recipes.CountAsync(), Is.EqualTo(recipeCount), "second run adds nothing");

		await context.Recipes.Where(r => r.Name == "Iron Ingot").ExecuteDeleteAsync();
		await import.ImportRecipesAsync();

		Assert.Multiple(async () => {
			Assert.That(await context.Parts.CountAsync(), Is.EqualTo(partCount));
			Assert.That(await context.Recipes.CountAsync(), Is.EqualTo(recipeCount));
			Assert.That(await context.Recipes.AnyAsync(r => r.Name == "Iron Ingot"), Is.True, "deleted recipe is re-added");
		});
	}

	[Test]
	public async Task Import_fills_in_missing_machines_on_existing_recipes() {
		using var db = await TestDatabase.CreateAsync();
		await using var context = db.CreateContext();
		var import = new DataImportService(context, await TestDatabase.GameInfoAsync(), NullLogger<DataImportService>.Instance);
		await context.Recipes.Where(r => r.Name == "Turbofuel Generator").ExecuteUpdateAsync(s => s.SetProperty(r => r.Machine, string.Empty));

		await import.ImportRecipesAsync();

		var generator = await context.Recipes.AsNoTracking().SingleAsync(r => r.Name == "Turbofuel Generator");
		Assert.That(generator.Machine, Is.EqualTo("Fuel-Powered Generator"));
	}

	[TestCase("Fuel-Powered Generator", 250)]
	[TestCase("Coal-Powered Generator", 75)]
	[TestCase("Nuclear Power Plant", 2500)]
	[TestCase("Smelter", null, Description = "consumers are not generators")]
	[TestCase("Storage Container", null, Description = "zero power is not a generator")]
	[TestCase("No Such Machine", null)]
	public async Task Generator_power_comes_from_the_machine_data(string machine, double? expected) {
		var gameInfo = await TestDatabase.GameInfoAsync();
		Assert.That(gameInfo.GetGeneratorPowerMW(machine), Is.EqualTo(expected));
	}
}
