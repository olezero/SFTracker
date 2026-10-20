using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Factory-specific override for a recipe input. If no record exists the input is enabled;
/// toggling creates a disabled record and flips it thereafter.
/// </summary>
[Table("FactoryRecipeInputOverrides")]
[Index(nameof(FactoryRecipeId), nameof(RecipeInputId), IsUnique = true)]
public class FactoryRecipeInputOverride {
	[Key]
	public int Id { get; set; }

	public int FactoryRecipeId { get; set; }
	public int RecipeInputId { get; set; }
	public bool IsEnabled { get; set; } = true;

	[ForeignKey(nameof(FactoryRecipeId))]
	public FactoryRecipe FactoryRecipe { get; set; } = null!;

	[ForeignKey(nameof(RecipeInputId))]
	public DbRecipeInput RecipeInput { get; set; } = null!;
}
