using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Network of factories within a game, can be used to group factories together.
/// Not surfaced in the UI yet.
/// </summary>
[Table("Networks")]
[Index(nameof(GameId), nameof(Name), IsUnique = true)]
[Index(nameof(GameId))]
public class Network {
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	public int GameId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	// Navigation properties
	[ForeignKey(nameof(GameId))]
	public Game Game { get; set; } = null!;

	[InverseProperty(nameof(Factory.Network))]
	public ICollection<Factory> Factories { get; set; } = [];
}
