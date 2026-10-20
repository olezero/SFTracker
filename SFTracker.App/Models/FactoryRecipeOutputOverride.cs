using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Factory-specific override for a recipe output. If no record exists the output is enabled;
/// toggling creates a disabled record and flips it thereafter.
/// </summary>
[Table("FactoryRecipeOutputOverrides")]
[Index(nameof(FactoryRecipeId), nameof(RecipeOutputId), IsUnique = true)]
public class FactoryRecipeOutputOverride {
	[Key]
	public int Id { get; set; }

	public int FactoryRecipeId { get; set; }
	public int RecipeOutputId { get; set; }
	public bool IsEnabled { get; set; } = true;

	[ForeignKey(nameof(FactoryRecipeId))]
	public FactoryRecipe FactoryRecipe { get; set; } = null!;

	[ForeignKey(nameof(RecipeOutputId))]
	public DbRecipeOutput RecipeOutput { get; set; } = null!;
}
