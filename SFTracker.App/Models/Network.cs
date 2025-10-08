using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Network of factories, can be used to group factories together
/// </summary>
[Table("Networks")]
[Index(nameof(Name), IsUnique = true)]
public class Network {
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	// Navigation properties
	[InverseProperty(nameof(Factory.Network))]
	public ICollection<Factory> Factories { get; set; } = [];
}
