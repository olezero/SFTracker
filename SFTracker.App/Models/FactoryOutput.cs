using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

[Table("FactoryOutputs")]
[Index(nameof(FactoryId), IsUnique = false)]
[Index(nameof(PartId), IsUnique = false)]
public class FactoryOutput {
	[Key]
	public int Id { get; set; }
	public int PartId { get; set; }
	public int FactoryId { get; set; }

	[Range(typeof(double), "0.001", "999999999", ErrorMessage = "Amount must be greater than 0")]
	public double AmountPerMinute { get; set; }

	[ForeignKey("PartId")]
	public DbPart Part { get; set; } = null!;

	[ForeignKey("FactoryId")]
	public Factory Factory { get; set; } = null!;
}
