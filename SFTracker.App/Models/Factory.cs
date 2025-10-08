using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

[Table("Factories")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(NetworkId))]
[Index(nameof(Sorting))]
public class Factory {
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	public double Sorting { get; set; } = 0.0;

	// Optional association with a Network
	public int? NetworkId { get; set; } = null;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	// Navigation properties
	[InverseProperty(nameof(FactoryInput.Factory))]
	public ICollection<FactoryInput> Inputs { get; set; } = [];

	[InverseProperty(nameof(FactoryOutput.Factory))]
	public ICollection<FactoryOutput> Outputs { get; set; } = [];

	[InverseProperty(nameof(FactoryRecipe.Factory))]
	public ICollection<FactoryRecipe> Recipes { get; set; } = [];

	[ForeignKey(nameof(NetworkId))]
	public Network Network { get; set; } = null!;
}
