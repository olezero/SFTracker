using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Represents factory-specific overrides for recipe outputs.
/// Only records that are disabled are stored - if no record exists, the output is enabled by default.
/// </summary>
[Table("FactoryRecipeOutputOverrides")]
[Index(nameof(FactoryRecipeId), nameof(RecipeOutputId), IsUnique = true)]
public class FactoryRecipeOutputOverride
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FactoryRecipeId { get; set; }
    public int RecipeOutputId { get; set; }
    public bool IsEnabled { get; set; } = true;

    [ForeignKey(nameof(FactoryRecipeId))]
    public FactoryRecipe FactoryRecipe { get; set; } = null!;

    [ForeignKey(nameof(RecipeOutputId))]
    public DbRecipeOutput RecipeOutput { get; set; } = null!;
}
