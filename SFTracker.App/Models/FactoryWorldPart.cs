using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// A part this factory gets from "the world" (some factory or network outside its own). The
/// resource balance lists such parts in a separate section with the all-factories totals rather
/// than the factory's (or its network's) own. Marked per factory, from the recipe editor.
/// </summary>
[Table("FactoryWorldParts")]
[Index(nameof(FactoryId), nameof(PartId), IsUnique = true)]
[Index(nameof(PartId))]
public class FactoryWorldPart {
	[Key]
	public int Id { get; set; }
	public int FactoryId { get; set; }
	public int PartId { get; set; }

	[ForeignKey(nameof(FactoryId))]
	public Factory Factory { get; set; } = null!;

	[ForeignKey(nameof(PartId))]
	public DbPart Part { get; set; } = null!;
}
