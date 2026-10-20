namespace SFTracker.Tests;

/// <summary>Builders for in-memory entity graphs (no database) with unique ids.</summary>
internal static class Build {
	private static int s_nextId = 1;

	public static DbPart Part(string name) => new() { Id = s_nextId++, Name = name };

	public static DbRecipe Recipe(string name, double timeSeconds, (DbPart part, double quantity)[] inputs, (DbPart part, double quantity)[] outputs) {
		var recipe = new DbRecipe { Id = s_nextId++, Name = name, TimeSeconds = timeSeconds };
		foreach (var (part, quantity) in inputs) {
			recipe.Inputs.Add(new DbRecipeInput { Id = s_nextId++, Part = part, PartId = part.Id, Recipe = recipe, RecipeId = recipe.Id, Quantity = quantity });
		}
		foreach (var (part, quantity) in outputs) {
			recipe.Outputs.Add(new DbRecipeOutput { Id = s_nextId++, Part = part, PartId = part.Id, Recipe = recipe, RecipeId = recipe.Id, Quantity = quantity });
		}
		return recipe;
	}

	public static FactoryRecipe FactoryRecipe(DbRecipe recipe, double multiplier, bool isSlooped = false) =>
		new() { Id = s_nextId++, Recipe = recipe, RecipeId = recipe.Id, Multiplier = multiplier, IsSlooped = isSlooped };

	public static Factory Factory(string name = "Factory") => new() { Id = s_nextId++, Name = name };

	public static FactoryInput Input(DbPart part, double amountPerMinute) =>
		new() { Id = s_nextId++, Part = part, PartId = part.Id, AmountPerMinute = amountPerMinute };

	public static FactoryOutput Output(DbPart part, double amountPerMinute) =>
		new() { Id = s_nextId++, Part = part, PartId = part.Id, AmountPerMinute = amountPerMinute };
}
