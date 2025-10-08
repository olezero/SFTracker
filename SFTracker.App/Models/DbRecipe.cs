using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

[Table("Recipes")]
[Index(nameof(Name), IsUnique = true)]
public class DbRecipe {
	[Key]
	public int Id { get; set; }
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	[MaxLength(500)]
	public string Tier { get; set; } = string.Empty;

	[Range(0, double.MaxValue)]
	public decimal TimeSeconds { get; set; }

	public bool IsAlternate { get; set; } = false;

	[InverseProperty(nameof(DbRecipeInput.Recipe))]
	public ICollection<DbRecipeInput> Inputs { get; set; } = [];

	[InverseProperty(nameof(DbRecipeOutput.Recipe))]
	public ICollection<DbRecipeOutput> Outputs { get; set; } = [];
}
