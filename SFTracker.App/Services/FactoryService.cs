using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

/// <summary>
/// Data access for factories and everything inside them (manual inputs/outputs, recipes, overrides).
/// </summary>
public class FactoryService {
	private readonly SFTrackerDbContext m_context;

	public FactoryService(SFTrackerDbContext context) {
		m_context = context;
	}

	/// <summary>
	/// Factories with everything the UI and the balance need: network, manual inputs/outputs (with parts)
	/// and active recipes with their inputs/outputs (with parts) and overrides.
	/// </summary>
	private IQueryable<Factory> FactoriesWithDetails() =>
		m_context.Factories
			.Include(f => f.Network)
			.Include(f => f.Inputs).ThenInclude(i => i.Part)
			.Include(f => f.Outputs).ThenInclude(o => o.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.Recipe).ThenInclude(r => r.Inputs).ThenInclude(ri => ri.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.Recipe).ThenInclude(r => r.Outputs).ThenInclude(ro => ro.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.InputOverrides)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.OutputOverrides)
			.Include(f => f.WorldParts).ThenInclude(w => w.Part)
			.AsSplitQuery();

	public async Task<List<Factory>> GetAllFactoriesAsync(int gameId) {
		return await FactoriesWithDetails()
			.Where(f => f.GameId == gameId)
			.OrderBy(f => f.Sorting).ThenBy(f => f.Id)
			.ToListAsync();
	}

	public async Task<Factory?> GetFactoryByIdAsync(int id) {
		return await FactoriesWithDetails().FirstOrDefaultAsync(f => f.Id == id);
	}

	public async Task<Factory> CreateFactoryAsync(string name, int gameId) {
		// Place new factories last in the manual ordering.
		var maxSorting = await m_context.Factories
			.Where(f => f.GameId == gameId)
			.MaxAsync(f => (double?)f.Sorting) ?? -1;

		var factory = new Factory { Name = name.Trim(), GameId = gameId, Sorting = maxSorting + 1 };
		m_context.Factories.Add(factory);
		await m_context.SaveChangesAsync();
		return factory;
	}

	/// <summary>
	/// Renames a factory and assigns it to a network (or none). Sets both the navigation and the FK so
	/// the tracked instance the UI holds stays consistent with what gets saved.
	/// </summary>
	public async Task UpdateFactoryAsync(int factoryId, string name, int? networkId) {
		var factory = await m_context.Factories.FindAsync(factoryId);
		if (factory == null) {
			return;
		}
		factory.Name = name.Trim();
		factory.Network = networkId.HasValue ? await m_context.Networks.FindAsync(networkId.Value) : null;
		factory.NetworkId = factory.Network?.Id;
		factory.UpdatedAt = DateTime.UtcNow;
		await m_context.SaveChangesAsync();
	}

	/// <summary>Persist a full manual ordering of a game's factories (Sorting = position in the list).</summary>
	public async Task ReorderFactoriesAsync(List<int> orderedFactoryIds) {
		var factories = await m_context.Factories
			.Where(f => orderedFactoryIds.Contains(f.Id))
			.ToListAsync();
		foreach (var factory in factories) {
			factory.Sorting = orderedFactoryIds.IndexOf(factory.Id);
		}
		await m_context.SaveChangesAsync();
	}

	public async Task DeleteFactoryAsync(int id) {
		var factory = await m_context.Factories.FindAsync(id);
		if (factory != null) {
			m_context.Factories.Remove(factory);
			await m_context.SaveChangesAsync();
		}
	}

	#region Manual inputs / outputs

	public async Task AddFactoryInputAsync(int factoryId, int partId, double amountPerMinute) {
		m_context.FactoryInputs.Add(new FactoryInput { FactoryId = factoryId, PartId = partId, AmountPerMinute = amountPerMinute });
		await m_context.SaveChangesAsync();
	}

	public async Task AddFactoryOutputAsync(int factoryId, int partId, double amountPerMinute) {
		m_context.FactoryOutputs.Add(new FactoryOutput { FactoryId = factoryId, PartId = partId, AmountPerMinute = amountPerMinute });
		await m_context.SaveChangesAsync();
	}

	public async Task UpdateFactoryInputAsync(int inputId, int partId, double amountPerMinute) {
		var input = await m_context.FactoryInputs.FindAsync(inputId);
		if (input != null) {
			input.PartId = partId;
			input.AmountPerMinute = amountPerMinute;
			await m_context.SaveChangesAsync();
		}
	}

	public async Task UpdateFactoryOutputAsync(int outputId, int partId, double amountPerMinute) {
		var output = await m_context.FactoryOutputs.FindAsync(outputId);
		if (output != null) {
			output.PartId = partId;
			output.AmountPerMinute = amountPerMinute;
			await m_context.SaveChangesAsync();
		}
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

	#endregion

	#region Recipes

	/// <returns>The newly created factory recipe (placed last in the manual ordering).</returns>
	public async Task<FactoryRecipe> AddFactoryRecipeAsync(int factoryId, int recipeId, double multiplier, bool isSlooped = false) {
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
		return factoryRecipe;
	}

	/// <param name="recipeId">
	/// Switch the row to a different recipe; null keeps the current one. Switching drops the row's
	/// input/output overrides, since they refer to the old recipe's inputs/outputs.
	/// </param>
	public async Task UpdateFactoryRecipeAsync(int factoryRecipeId, double multiplier, bool isSlooped, int? recipeId = null) {
		var factoryRecipe = await m_context.FactoryRecipes.FindAsync(factoryRecipeId);
		if (factoryRecipe == null) {
			return;
		}
		if (recipeId.HasValue && recipeId.Value != factoryRecipe.RecipeId) {
			factoryRecipe.RecipeId = recipeId.Value;
			await m_context.FactoryRecipeInputOverrides.Where(o => o.FactoryRecipeId == factoryRecipeId).ExecuteDeleteAsync();
			await m_context.FactoryRecipeOutputOverrides.Where(o => o.FactoryRecipeId == factoryRecipeId).ExecuteDeleteAsync();
		}
		factoryRecipe.Multiplier = multiplier;
		factoryRecipe.IsSlooped = isSlooped;
		await m_context.SaveChangesAsync();
	}

	/// <summary>
	/// Persist a full manual ordering of a factory's recipes. Sorting is assigned
	/// from the position of each id in the supplied list.
	/// </summary>
	public async Task ReorderFactoryRecipesAsync(List<int> orderedFactoryRecipeIds) {
		var recipes = await m_context.FactoryRecipes
			.Where(r => orderedFactoryRecipeIds.Contains(r.Id))
			.ToListAsync();
		foreach (var recipe in recipes) {
			recipe.Sorting = orderedFactoryRecipeIds.IndexOf(recipe.Id);
		}
		await m_context.SaveChangesAsync();
	}

	/// <summary>
	/// Move a recipe row to another factory, placing it last in that factory's manual order. The
	/// row's input/output overrides travel with it (they refer to the recipe, not the factory).
	/// </summary>
	public async Task MoveFactoryRecipeAsync(int factoryRecipeId, int targetFactoryId) {
		var factoryRecipe = await m_context.FactoryRecipes.FindAsync(factoryRecipeId);
		if (factoryRecipe == null || factoryRecipe.FactoryId == targetFactoryId) {
			return;
		}
		var maxSorting = await m_context.FactoryRecipes
			.Where(r => r.FactoryId == targetFactoryId && r.IsActive)
			.MaxAsync(r => (double?)r.Sorting) ?? -1;
		factoryRecipe.FactoryId = targetFactoryId;
		factoryRecipe.Sorting = maxSorting + 1;
		await m_context.SaveChangesAsync();
	}

	public async Task RemoveFactoryRecipeAsync(int factoryRecipeId) {
		var factoryRecipe = await m_context.FactoryRecipes.FindAsync(factoryRecipeId);
		if (factoryRecipe != null) {
			m_context.FactoryRecipes.Remove(factoryRecipe);
			await m_context.SaveChangesAsync();
		}
	}

	/// <summary>
	/// Toggle whether a recipe input counts toward the balance. No override = enabled, so the first
	/// toggle creates a disabled override; later toggles flip it.
	/// </summary>
	public async Task ToggleRecipeInputAsync(int factoryRecipeId, int recipeInputId) {
		var existing = await m_context.FactoryRecipeInputOverrides
			.FirstOrDefaultAsync(o => o.FactoryRecipeId == factoryRecipeId && o.RecipeInputId == recipeInputId);
		if (existing == null) {
			m_context.FactoryRecipeInputOverrides.Add(new FactoryRecipeInputOverride {
				FactoryRecipeId = factoryRecipeId,
				RecipeInputId = recipeInputId,
				IsEnabled = false
			});
		} else {
			existing.IsEnabled = !existing.IsEnabled;
		}
		await m_context.SaveChangesAsync();
	}

	/// <summary>Output counterpart of <see cref="ToggleRecipeInputAsync"/>.</summary>
	public async Task ToggleRecipeOutputAsync(int factoryRecipeId, int recipeOutputId) {
		var existing = await m_context.FactoryRecipeOutputOverrides
			.FirstOrDefaultAsync(o => o.FactoryRecipeId == factoryRecipeId && o.RecipeOutputId == recipeOutputId);
		if (existing == null) {
			m_context.FactoryRecipeOutputOverrides.Add(new FactoryRecipeOutputOverride {
				FactoryRecipeId = factoryRecipeId,
				RecipeOutputId = recipeOutputId,
				IsEnabled = false
			});
		} else {
			existing.IsEnabled = !existing.IsEnabled;
		}
		await m_context.SaveChangesAsync();
	}

	/// <summary>
	/// Marks or unmarks a part as coming from outside the factory's network ("world"): the balance
	/// then lists it with the all-factories totals. Per factory, so it covers every recipe using it.
	/// </summary>
	public async Task ToggleWorldPartAsync(int factoryId, int partId) {
		var existing = await m_context.FactoryWorldParts
			.FirstOrDefaultAsync(w => w.FactoryId == factoryId && w.PartId == partId);
		if (existing == null) {
			m_context.FactoryWorldParts.Add(new FactoryWorldPart { FactoryId = factoryId, PartId = partId });
		} else {
			m_context.FactoryWorldParts.Remove(existing);
		}
		await m_context.SaveChangesAsync();
	}

	#endregion

	#region Game reference data

	public async Task<List<DbPart>> GetAllPartsAsync() {
		return await m_context.Parts
			.Where(p => !p.Name.Contains("FICSMAS"))
			.OrderBy(p => p.Name)
			.ToListAsync();
	}

	public async Task<List<DbRecipe>> GetAllRecipesAsync() {
		return await m_context.Recipes
			.Where(r => !r.Name.Contains("FICSMAS"))
			.Include(r => r.Inputs).ThenInclude(i => i.Part)
			.Include(r => r.Outputs).ThenInclude(o => o.Part)
			.OrderBy(r => r.Name)
			.AsSplitQuery()
			.ToListAsync();
	}

	#endregion
}
