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
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public int FactoryId { get; set; }
	public int RecipeId { get; set; }
	public decimal Multiplier { get; set; }
	public bool IsActive { get; set; } = true;

	[ForeignKey(nameof(FactoryId))]
	public Factory Factory { get; set; } = null!;

	[ForeignKey(nameof(RecipeId))]
	public DbRecipe Recipe { get; set; } = null!;

	// Navigation properties for overrides
	public ICollection<FactoryRecipeInputOverride> InputOverrides { get; set; } = new List<FactoryRecipeInputOverride>();
	public ICollection<FactoryRecipeOutputOverride> OutputOverrides { get; set; } = new List<FactoryRecipeOutputOverride>();
}
