using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// A Satisfactory save/playthrough. Everything the user plans (factories, networks) belongs to exactly one game.
/// </summary>
[Table("Games")]
[Index(nameof(Name), IsUnique = true)]
public class Game {
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Bumped whenever the game is selected in the UI; the most recent one is opened on startup.
	/// </summary>
	public DateTime LastOpenedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Divisor applied to every fluid quantity in every recipe (for the fluid-scaling mod; 1 = vanilla).
	/// Applied at calculation time in DbRecipeInput/Output.GetQuantityPerMinute, never to stored data.
	/// </summary>
	public int FluidDivisor { get; set; } = 1;

	// Navigation properties
	[InverseProperty(nameof(Factory.Game))]
	public ICollection<Factory> Factories { get; set; } = [];

	[InverseProperty(nameof(Network.Game))]
	public ICollection<Network> Networks { get; set; } = [];
}
