using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

[Table("RecipeOutputs")]
[Index(nameof(RecipeId), nameof(PartId), IsUnique = true)]
public class DbRecipeOutput {
	[Key]
	public int Id { get; set; }
	public double Quantity { get; set; }

	public int RecipeId { get; set; }
	public int PartId { get; set; }

	[ForeignKey(nameof(PartId))]
	public DbPart Part { get; set; } = null!;

	[ForeignKey(nameof(RecipeId))]
	public DbRecipe Recipe { get; set; } = null!;

	public double GetQuantityPerMinute(double multiplier, bool isSlooped = false) => Quantity * multiplier / Recipe.TimeSeconds * 60 * (isSlooped ? 2 : 1);
	public double GetQuantityPerMinute(FactoryRecipe factoryRecipe) => Quantity * factoryRecipe.Multiplier / Recipe.TimeSeconds * 60 * (factoryRecipe.IsSlooped ? 2 : 1);
}
