using Microsoft.EntityFrameworkCore;

namespace SFTracker.Tests;

public class SchemaUpgraderTests {
	[Test]
	public async Task A_current_database_needs_no_steps() {
		using var db = await TestDatabase.CreateAsync(seedGameData: false);
		await using var context = db.CreateContext();

		Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.Zero);
	}

	[Test]
	public async Task Missing_columns_are_added_and_the_upgrade_is_idempotent() {
		using var db = await TestDatabase.CreateAsync(seedGameData: false);
		await using var context = db.CreateContext();
		// Simulate a database created before the column existed.
		await context.Database.ExecuteSqlRawAsync("ALTER TABLE Recipes DROP COLUMN Machine");
		await context.Database.ExecuteSqlRawAsync("INSERT INTO Recipes (Name, Tier, TimeSeconds, IsAlternate) VALUES ('Old', '', 1, 0)");

		Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.EqualTo(1));

		context.Recipes.Add(new DbRecipe { Name = "After", Machine = "Smelter", TimeSeconds = 1 });
		await context.SaveChangesAsync();
		Assert.Multiple(async () => {
			Assert.That((await context.Recipes.AsNoTracking().SingleAsync(r => r.Name == "Old")).Machine, Is.Empty, "existing rows get the column default");
			Assert.That((await context.Recipes.AsNoTracking().SingleAsync(r => r.Name == "After")).Machine, Is.EqualTo("Smelter"));
			Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.Zero, "second run is a no-op");
		});
	}
}
