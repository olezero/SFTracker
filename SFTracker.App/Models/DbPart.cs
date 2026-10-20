using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

[Table("Parts")]
[Index(nameof(Name), IsUnique = true)]
public class DbPart {
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	[MaxLength(50)]
	public string? Tier { get; set; }

	public int SinkPoints { get; set; }

	[MaxLength(500)]
	public string? ImageUrl { get; set; }

	/// <summary>True for piped parts (liquids/gases); these are scaled by <see cref="Game.FluidDivisor"/>.</summary>
	[NotMapped]
	public bool IsFluid => Constants.FluidParts.IsFluid(Name);
}
