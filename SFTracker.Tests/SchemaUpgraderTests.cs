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

	[Test]
	public async Task Game_settings_column_is_added_and_existing_games_get_default_settings() {
		using var db = await TestDatabase.CreateAsync(seedGameData: false);
		await using var context = db.CreateContext();
		await context.Database.ExecuteSqlRawAsync("ALTER TABLE Games DROP COLUMN Settings");
		await context.Database.ExecuteSqlRawAsync("INSERT INTO Games (Name, CreatedAt, UpdatedAt, LastOpenedAt, FluidDivisor) VALUES ('Old', '2020-01-01', '2020-01-01', '2020-01-01', 1)");

		Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.EqualTo(1));

		var old = await context.Games.AsNoTracking().SingleAsync(g => g.Name == "Old");
		Assert.Multiple(async () => {
			Assert.That(old.Settings.MachineSpeeds, Is.Empty);
			Assert.That(old.Settings.SpeedOf("Constructor"), Is.EqualTo(1));
			Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.Zero, "second run is a no-op");
		});
	}

	[Test]
	public async Task Missing_tables_are_created_exactly_as_in_a_fresh_database() {
		using var db = await TestDatabase.CreateAsync(seedGameData: false);
		await using var context = db.CreateContext();
		var fresh = await SchemaOfAsync(context, "FactoryWorldParts");
		// Simulate a database created before the table existed.
		await context.Database.ExecuteSqlRawAsync("DROP TABLE FactoryWorldParts");

		Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.EqualTo(1));

		Assert.Multiple(async () => {
			Assert.That(await SchemaOfAsync(context, "FactoryWorldParts"), Is.EqualTo(fresh), "table and indexes match EnsureCreated");
			Assert.That(await SchemaUpgrader.UpgradeAsync(context), Is.Zero, "second run is a no-op");
		});
	}

	/// <summary>The table's and its indexes' DDL as SQLite stores it, with whitespace removed.</summary>
	private static async Task<List<string>> SchemaOfAsync(Data.SFTrackerDbContext context, string table) {
		var statements = await context.Database
			.SqlQuery<string>($"SELECT sql AS \"Value\" FROM sqlite_master WHERE tbl_name = {table} AND sql IS NOT NULL ORDER BY type, name")
			.ToListAsync();
		return statements.Select(sql => string.Concat(sql.Where(c => !char.IsWhiteSpace(c)))).ToList();
	}
}
