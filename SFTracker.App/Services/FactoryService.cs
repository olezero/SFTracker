using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

public class FactoryService {
	private readonly SFTrackerDbContext m_context;

	public FactoryService(SFTrackerDbContext context) {
		m_context = context;
	}

	public async Task<List<Factory>> GetAllFactoriesAsync(int gameId) {
		return await m_context.Factories
			.Where(f => f.GameId == gameId)
			.Include(f => f.Network)
			.Include(f => f.Inputs)
				.ThenInclude(i => i.Part)
				.OrderBy(i => i.Name)
			.Include(f => f.Outputs)
				.ThenInclude(o => o.Part)
				.OrderBy(i => i.Name)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.Recipe)
				.ThenInclude(r => r.Inputs)
				.ThenInclude(ri => ri.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.Recipe)
				.ThenInclude(r => r.Outputs)
				.ThenInclude(ro => ro.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.InputOverrides)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.OutputOverrides)
			.OrderBy(f => f.Sorting)
			.AsSplitQuery()
			.ToListAsync();
	}

	public async Task<Factory?> GetFactoryByIdAsync(int id) {
		return await m_context.Factories
			.Include(f => f.Network)
			.Include(f => f.Inputs)
				.ThenInclude(i => i.Part)
				.OrderBy(i => i.Name)
			.Include(f => f.Outputs)
				.ThenInclude(o => o.Part)
				.OrderBy(o => o.Name)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.Recipe)
				.ThenInclude(r => r.Inputs)
				.ThenInclude(ri => ri.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.Recipe)
				.ThenInclude(r => r.Outputs)
				.ThenInclude(ro => ro.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.InputOverrides)
			.Include(f => f.Recipes.Where(r => r.IsActive))
				.ThenInclude(r => r.OutputOverrides)
			.AsSplitQuery()
			.FirstOrDefaultAsync(f => f.Id == id);
	}

	public async Task<Factory> CreateFactoryAsync(string name, int gameId) {
		var factory = new Factory { Name = name, GameId = gameId };
		m_context.Factories.Add(factory);
		await m_context.SaveChangesAsync();
		return factory;
	}

	/// <summary>
	/// Assigns a factory to a network (or none). Sets both the navigation and the FK so the
	/// tracked instance stays consistent with what gets saved.
	/// </summary>
	public async Task SetFactoryNetworkAsync(int factoryId, int? networkId) {
		var factory = await m_context.Factories.FindAsync(factoryId);
		if (factory == null) {
			return;
		}
		factory.Network = networkId.HasValue ? await m_context.Networks.FindAsync(networkId.Value) : null;
		factory.NetworkId = factory.Network?.Id;
		factory.UpdatedAt = DateTime.UtcNow;
		await m_context.SaveChangesAsync();
	}

	public async Task<Factory> UpdateFactoryAsync(Factory factory) {
		factory.UpdatedAt = DateTime.UtcNow;
		m_context.Factories.Update(factory);
		await m_context.SaveChangesAsync();
		return factory;
	}

	public async Task DeleteFactoryAsync(int id) {
		var factory = await m_context.Factories.FindAsync(id);
		if (factory != null) {
			m_context.Factories.Remove(factory);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task AddFactoryInputAsync(int factoryId, int partId, double amountPerMinute) {
		var input = new FactoryInput {
			FactoryId = factoryId,
			PartId = partId,
			AmountPerMinute = amountPerMinute
		};
		m_context.FactoryInputs.Add(input);
		await m_context.SaveChangesAsync();
	}

	public async Task AddFactoryOutputAsync(int factoryId, int partId, double amountPerMinute) {
		var output = new FactoryOutput {
			FactoryId = factoryId,
			PartId = partId,
			AmountPerMinute = amountPerMinute
		};
		m_context.FactoryOutputs.Add(output);
		await m_context.SaveChangesAsync();
	}

	public async Task RemoveFactoryInputAsync(int inputId) {
		var input = await m_context.FactoryInputs.FindAsync(inputId);
		if (input != null) {
			m_context.FactoryInputs.Remove(input);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task RemoveFactoryOutputAsync(int outputId) {
		var output = await m_context.FactoryOutputs.FindAsync(outputId);
		if (output != null) {
			m_context.FactoryOutputs.Remove(output);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task AddFactoryRecipeAsync(int factoryId, int recipeId, double multiplier, bool isSlooped = false) {
		// Place new recipes last in the manual ordering.
		var maxSorting = await m_context.FactoryRecipes
			.Where(r => r.FactoryId == factoryId && r.IsActive)
			.MaxAsync(r => (double?)r.Sorting) ?? -1;

		var factoryRecipe = new FactoryRecipe {
			FactoryId = factoryId,
			RecipeId = recipeId,
			Multiplier = multiplier,
			IsSlooped = isSlooped,
			IsActive = true,
			Sorting = maxSorting + 1
		};
		m_context.FactoryRecipes.Add(factoryRecipe);
		await m_context.SaveChangesAsync();
	}

	public async Task UpdateFactoryRecipeAsync(int factoryRecipeId, double multiplier, bool isSlooped = false) {
		var factoryRecipe = await m_context.FactoryRecipes.FindAsync(factoryRecipeId);
		if (factoryRecipe != null) {
			factoryRecipe.Multiplier = multiplier;
			factoryRecipe.IsSlooped = isSlooped;
			m_context.FactoryRecipes.Update(factoryRecipe);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task UpdateFactoryRecipeAsync(int factoryRecipeId, int recipeId, double multiplier, bool isSlooped = false) {
		var factoryRecipe = await m_context.FactoryRecipes.FindAsync(factoryRecipeId);
		if (factoryRecipe != null) {
			factoryRecipe.RecipeId = recipeId;
			factoryRecipe.Multiplier = multiplier;
			factoryRecipe.IsSlooped = isSlooped;
			m_context.FactoryRecipes.Update(factoryRecipe);
			await m_context.SaveChangesAsync();
		}
	}

	/// <summary>
	/// Persist a full manual ordering of a factory's recipes. Sorting is assigned
	/// from the position of each id in the supplied list.
	/// </summary>
	public async Task ReorderFactoryRecipesAsync(List<int> orderedFactoryRecipeIds) {
		var recipes = await m_context.FactoryRecipes
			.Where(r => orderedFactoryRecipeIds.Contains(r.Id))
			.ToListAsync();

		for (int i = 0; i < orderedFactoryRecipeIds.Count; i++) {
			var recipe = recipes.FirstOrDefault(r => r.Id == orderedFactoryRecipeIds[i]);
			if (recipe != null) {
				recipe.Sorting = i;
			}
		}

		await m_context.SaveChangesAsync();
	}

	public async Task RemoveFactoryRecipeAsync(int factoryRecipeId) {
		var factoryRecipe = await m_context.FactoryRecipes.FindAsync(factoryRecipeId);
		if (factoryRecipe != null) {
			m_context.FactoryRecipes.Remove(factoryRecipe);
			await m_context.SaveChangesAsync();
		}
	}

	public async Task<List<DbPart>> GetAllPartsAsync() {
		return await m_context.Parts
			.Where(p => !p.Name.Contains("FICSMAS"))
			.OrderBy(p => p.Name)
			.ToListAsync();
	}

	// TODO - this should only returnt he recipe, need a seperate method to get recipe details methinks - or we could just cache the result
	public async Task<List<DbRecipe>> GetAllRecipesAsync() {
		return await m_context.Recipes
			.Where(n => !n.Name.Contains("FICSMAS"))

			.Include(r => r.Inputs)
			.ThenInclude(rp => rp.Part)

			.Include(r => r.Outputs)
			.ThenInclude(rp => rp.Part)

			.OrderBy(r => r.Name)
			.AsSplitQuery()
			.ToListAsync();
	}

	/// <summary>
	/// Toggle whether a recipe input is enabled/disabled for balance calculations
	/// </summary>
	public async Task ToggleRecipeInputAsync(int factoryRecipeId, int recipeInputId) {
		var existingOverride = await m_context.FactoryRecipeInputOverrides
			.FirstOrDefaultAsync(o => o.FactoryRecipeId == factoryRecipeId && o.RecipeInputId == recipeInputId);

		if (existingOverride == null) {
			// No override exists, so input is currently enabled - create disabled override
			var newOverride = new FactoryRecipeInputOverride {
				FactoryRecipeId = factoryRecipeId,
				RecipeInputId = recipeInputId,
				IsEnabled = false
			};
			m_context.FactoryRecipeInputOverrides.Add(newOverride);
		} else {
			// Override exists, toggle its state
			existingOverride.IsEnabled = !existingOverride.IsEnabled;
			m_context.FactoryRecipeInputOverrides.Update(existingOverride);
		}

		await m_context.SaveChangesAsync();
	}

	/// <summary>
	/// Toggle whether a recipe output is enabled/disabled for balance calculations
	/// </summary>
	public async Task ToggleRecipeOutputAsync(int factoryRecipeId, int recipeOutputId) {
		var existingOverride = await m_context.FactoryRecipeOutputOverrides
			.FirstOrDefaultAsync(o => o.FactoryRecipeId == factoryRecipeId && o.RecipeOutputId == recipeOutputId);

		if (existingOverride == null) {
			// No override exists, so output is currently enabled - create disabled override
			var newOverride = new FactoryRecipeOutputOverride {
				FactoryRecipeId = factoryRecipeId,
				RecipeOutputId = recipeOutputId,
				IsEnabled = false
			};
			m_context.FactoryRecipeOutputOverrides.Add(newOverride);
		} else {
			// Override exists, toggle its state
			existingOverride.IsEnabled = !existingOverride.IsEnabled;
			m_context.FactoryRecipeOutputOverrides.Update(existingOverride);
		}

		await m_context.SaveChangesAsync();
	}

	/// <summary>
	/// Check if a recipe input is enabled for balance calculations
	/// </summary>
	public bool IsRecipeInputEnabled(FactoryRecipe factoryRecipe, int recipeInputId) {
		var override_ = factoryRecipe.InputOverrides
			.FirstOrDefault(o => o.RecipeInputId == recipeInputId);
		return override_?.IsEnabled ?? true; // Default to enabled if no override exists
	}

	/// <summary>
	/// Check if a recipe output is enabled for balance calculations
	/// </summary>
	public bool IsRecipeOutputEnabled(FactoryRecipe factoryRecipe, int recipeOutputId) {
		var override_ = factoryRecipe.OutputOverrides
			.FirstOrDefault(o => o.RecipeOutputId == recipeOutputId);
		return override_?.IsEnabled ?? true; // Default to enabled if no override exists
	}
}
