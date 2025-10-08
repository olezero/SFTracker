using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

[Table("RecipeInputs")]
[Index(nameof(RecipeId), nameof(PartId), IsUnique = true)]
public class DbRecipeInput {
	[Key]
	public int Id { get; set; }
	public double Quantity { get; set; }

	public int RecipeId { get; set; }
	public int PartId { get; set; }

	[ForeignKey(nameof(PartId))]
	public DbPart Part { get; set; } = null!;

	[ForeignKey(nameof(RecipeId))]
	public DbRecipe Recipe { get; set; } = null!;

	public double GetQuantityPerMinute(double multiplier) => Quantity * multiplier / Recipe.TimeSeconds * 60;
	public double GetQuantityPerMinute(FactoryRecipe factoryRecipe) => Quantity * factoryRecipe.Multiplier / Recipe.TimeSeconds * 60;
}
