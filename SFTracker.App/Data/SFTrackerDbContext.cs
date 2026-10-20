using Microsoft.EntityFrameworkCore;
using SFTracker.Models;

namespace SFTracker.Data;

public class SFTrackerDbContext(DbContextOptions<SFTrackerDbContext> options) : DbContext(options) {
	// Game reference data (seeded from game_data.json)
	public DbSet<DbPart> Parts { get; set; }
	public DbSet<DbRecipe> Recipes { get; set; }
	public DbSet<DbRecipeInput> RecipeInputs { get; set; }
	public DbSet<DbRecipeOutput> RecipeOutputs { get; set; }

	// User data
	public DbSet<Game> Games { get; set; }
	public DbSet<Network> Networks { get; set; }
	public DbSet<Factory> Factories { get; set; }
	public DbSet<FactoryInput> FactoryInputs { get; set; }
	public DbSet<FactoryOutput> FactoryOutputs { get; set; }
	public DbSet<FactoryRecipe> FactoryRecipes { get; set; }
	public DbSet<FactoryRecipeInputOverride> FactoryRecipeInputOverrides { get; set; }
	public DbSet<FactoryRecipeOutputOverride> FactoryRecipeOutputOverrides { get; set; }
}
