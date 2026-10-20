using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Pivot between recipes and factories, with a multiplier for production speed.
/// For instance a given factory can have 10 machines that are overclocked to 250%, which would result in a multiplier of 25.
/// This allows us to track multiple recipes per factory, and the production rate of each.
/// </summary>
[Table("FactoryRecipes")]
[Index(nameof(FactoryId), nameof(RecipeId), IsUnique = false)]
[Index(nameof(RecipeId))]
[Index(nameof(FactoryId))]
[Index(nameof(IsActive))]
public class FactoryRecipe {
	[Key]
	public int Id { get; set; }

	public int FactoryId { get; set; }
	public int RecipeId { get; set; }
	public double Multiplier { get; set; }
	public bool IsActive { get; set; } = true;
	public bool IsSlooped { get; set; } = false;

	/// <summary>
	/// Manual ordering of recipes within a factory (ascending). Ties fall back to Id.
	/// </summary>
	public double Sorting { get; set; } = 0.0;

	[ForeignKey(nameof(FactoryId))]
	public Factory Factory { get; set; } = null!;

	[ForeignKey(nameof(RecipeId))]
	public DbRecipe Recipe { get; set; } = null!;

	// Per-recipe toggles for whether an input/output counts toward the balance.
	public ICollection<FactoryRecipeInputOverride> InputOverrides { get; set; } = [];
	public ICollection<FactoryRecipeOutputOverride> OutputOverrides { get; set; } = [];

	/// <summary>Whether a recipe input counts toward the balance. No override = enabled.</summary>
	public bool IsInputEnabled(int recipeInputId) =>
		InputOverrides.FirstOrDefault(o => o.RecipeInputId == recipeInputId)?.IsEnabled ?? true;

	/// <summary>Whether a recipe output counts toward the balance. No override = enabled.</summary>
	public bool IsOutputEnabled(int recipeOutputId) =>
		OutputOverrides.FirstOrDefault(o => o.RecipeOutputId == recipeOutputId)?.IsEnabled ?? true;
}
