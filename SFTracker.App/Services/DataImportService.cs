using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

/// <summary>
/// Seeds the Parts/Recipes tables from game_data.json. Idempotent: parts and recipes that already exist
/// (by name) are left alone, so entries added to game_data.json land on the next start.
/// </summary>
public class DataImportService {
	private readonly SFTrackerDbContext m_context;
	private readonly GameInfoService m_gameInfoService;
	private readonly ILogger<DataImportService> m_logger;

	public DataImportService(SFTrackerDbContext context, GameInfoService gameInfoService, ILogger<DataImportService> logger) {
		m_context = context;
		m_gameInfoService = gameInfoService;
		m_logger = logger;
	}

	public async Task ImportPartsAsync() {
		var existing = await m_context.Parts.Select(p => p.Name).ToHashSetAsync();
		var newParts = m_gameInfoService.GameData.Parts
			.Where(p => !existing.Contains(p.Name))
			.Select(p => new DbPart {
				Name = p.Name,
				Tier = p.Tier,
				SinkPoints = p.SinkPoints,
				ImageUrl = p.ImageUrl
			})
			.ToList();
		if (newParts.Count == 0) {
			return;
		}

		m_context.Parts.AddRange(newParts);
		await m_context.SaveChangesAsync();
		m_logger.LogInformation("Imported {Count} parts from game data.", newParts.Count);
	}

	public async Task ImportRecipesAsync() {
		var existing = await m_context.Recipes.Select(r => r.Name).ToHashSetAsync();
		var partIdsByName = await m_context.Parts.ToDictionaryAsync(p => p.Name, p => p.Id);

		var newRecipes = new List<DbRecipe>();
		foreach (var recipe in m_gameInfoService.GameData.Recipes.Where(r => !existing.Contains(r.Name))) {
			var dbRecipe = new DbRecipe {
				Name = recipe.Name,
				Tier = recipe.Tier ?? string.Empty,
				TimeSeconds = ParseNumber(recipe.BatchTime),
				IsAlternate = recipe.Alternate ?? false
			};
			foreach (var part in recipe.Parts) {
				if (!partIdsByName.TryGetValue(part.Part, out var partId)) {
					throw new InvalidOperationException($"Recipe '{recipe.Name}' references unknown part '{part.Part}'.");
				}
				// Negative amounts are inputs, positive amounts are outputs.
				var quantity = ParseNumber(part.Amount);
				if (quantity < 0) {
					dbRecipe.Inputs.Add(new DbRecipeInput { PartId = partId, Quantity = -quantity });
				} else {
					dbRecipe.Outputs.Add(new DbRecipeOutput { PartId = partId, Quantity = quantity });
				}
			}
			newRecipes.Add(dbRecipe);
		}
		if (newRecipes.Count == 0) {
			return;
		}

		m_context.Recipes.AddRange(newRecipes);
		await m_context.SaveChangesAsync();
		m_logger.LogInformation("Imported {Count} recipes from game data.", newRecipes.Count);
	}

	/// <summary>Parses a game-data number, which is either plain ("30", "-1") or a fraction ("12/5", "-9/5").</summary>
	public static double ParseNumber(string value) {
		var parts = value.Split('/');
		if (parts.Length == 1 && tryParse(parts[0], out var number)) {
			return number;
		}
		if (parts.Length == 2 && tryParse(parts[0], out var numerator) && tryParse(parts[1], out var denominator) && denominator != 0) {
			return numerator / denominator;
		}
		throw new FormatException($"Invalid number in game data: '{value}'");

		static bool tryParse(string s, out double result) =>
			double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}
}
