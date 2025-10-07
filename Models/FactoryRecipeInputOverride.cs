using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SFTracker.Models;

/// <summary>
/// Represents factory-specific overrides for recipe inputs.
/// Only records that are disabled are stored - if no record exists, the input is enabled by default.
/// </summary>
[Table("FactoryRecipeInputOverrides")]
[Index(nameof(FactoryRecipeId), nameof(RecipeInputId), IsUnique = true)]
public class FactoryRecipeInputOverride
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FactoryRecipeId { get; set; }
    public int RecipeInputId { get; set; }
    public bool IsEnabled { get; set; } = true;

    [ForeignKey(nameof(FactoryRecipeId))]
    public FactoryRecipe FactoryRecipe { get; set; } = null!;

    [ForeignKey(nameof(RecipeInputId))]
    public DbRecipeInput RecipeInput { get; set; } = null!;
}
