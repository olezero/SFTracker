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

	/// <param name="fluidDivisor">The game's fluid divisor; only applied when the part is a fluid.</param>
	public double GetQuantityPerMinute(double multiplier, int fluidDivisor = 1) =>
		Quantity * multiplier / Recipe.TimeSeconds * 60 / FluidScale(fluidDivisor);
	public double GetQuantityPerMinute(FactoryRecipe factoryRecipe, int fluidDivisor = 1) =>
		GetQuantityPerMinute(factoryRecipe.Multiplier, fluidDivisor);

	private double FluidScale(int fluidDivisor) => Part?.IsFluid == true && fluidDivisor > 0 ? fluidDivisor : 1;
}
