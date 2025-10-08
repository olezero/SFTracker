using System.Text;
using SFTracker.Data;
using SFTracker.Models;
using SFTracker.Services.Data;

namespace SFTracker.Services;

public class DataImportService {
	private readonly SFTrackerDbContext m_context;
	private readonly GameInfoService m_gameInfoService;

	public DataImportService(SFTrackerDbContext context, GameInfoService gameInfoService) {
		m_context = context;
		m_gameInfoService = gameInfoService;
	}

	public async Task ImportPartsAsync() {
		// Check if parts are already imported
		if (m_context.Parts.Any()) {
			Console.WriteLine("Parts already imported, skipping...");
			return;
		}

		Console.WriteLine("Importing parts from game data...");

		var gameData = m_gameInfoService.GameData;

		foreach (var part in gameData.Parts) {
			var dbPart = new DbPart {
				Name = part.Name,
				Tier = part.Tier,
				SinkPoints = part.SinkPoints,
				ImageUrl = part.ImageUrl
			};

			m_context.Parts.Add(dbPart);
		}

		await m_context.SaveChangesAsync();
		Console.WriteLine($"Imported {gameData.Parts.Count} parts successfully.");
	}

	public async Task ImportRecipesAsync() {
		if (m_context.Recipes.Any()) {
			Console.WriteLine("Recipes already imported, skipping...");
			return;
		}

		// Ensure there are parts in the database
		if (!m_context.Parts.Any()) {
			throw new InvalidOperationException("No parts found in database. Please import parts before importing recipes.");
		}

		var transaction = await m_context.Database.BeginTransactionAsync();
		try {
			Console.WriteLine("Importing recipes from game data...");
			var gameData = m_gameInfoService.GameData;
			foreach (var recipe in gameData.Recipes) {
				if (recipe.BatchTime.Contains('/')) {
					Console.WriteLine($"Skipping recipe with fractional batch time for now: {recipe.Name} with BatchTime {recipe.BatchTime}");
					continue;
				}

				var dbRecipe = new DbRecipe {
					Name = recipe.Name,
					Tier = recipe.Tier ?? string.Empty,
					TimeSeconds = double.Parse(recipe.BatchTime),
					IsAlternate = recipe.Alternate ?? false
				};

				// Add the recipe to context first to get the ID
				m_context.Recipes.Add(dbRecipe);
				await m_context.SaveChangesAsync(); // Save to get the RecipeId

				foreach (var part in recipe.Parts) {
					var partName = part.Part;
					var dbPart = m_context.Parts.FirstOrDefault(p => p.Name == partName);
					if (dbPart == null) {
						throw new InvalidOperationException($"Part '{partName}' not found in database. Ensure all parts are imported before importing recipes.");
					}
					var quantity = ParseFractions(part.Amount);
					var isInput = quantity < 0;

					if (isInput) {
						var recipeInput = new DbRecipeInput {
							PartId = dbPart.Id,
							Quantity = Math.Abs(quantity) // Store as positive quantity
						};
						dbRecipe.Inputs.Add(recipeInput);
					} else {
						var recipeOutput = new DbRecipeOutput {
							PartId = dbPart.Id,
							Quantity = quantity
						};
						dbRecipe.Outputs.Add(recipeOutput);
					}

				}
			}

			await m_context.SaveChangesAsync();
		} catch (Exception ex) {
			await transaction.RollbackAsync();
			Console.WriteLine($"Error importing recipes: {ex.Message}");
			throw;
		}

		await transaction.CommitAsync();
		Console.WriteLine("Recipes imported successfully.");
	}

	private double ParseFractions(string batchTime) {
		// Can be in 1/2 or 1/4 etc format, usually just a pure number
		if (batchTime.Contains('/')) {
			Console.WriteLine($"Parsing fractional batch time: {batchTime}");
			var parts = batchTime.Split('/');
			if (parts.Length == 2 && double.TryParse(parts[0], out var numerator) && double.TryParse(parts[1], out var denominator) && denominator != 0) {
				var parsedValue = numerator / denominator;
				Console.WriteLine($"Parsed {batchTime} as {parsedValue}");
				return parsedValue;
			}
			throw new FormatException($"Invalid batch time format: {batchTime}");
		} else if (double.TryParse(batchTime, out var result)) {
			return result;
		} else {
			throw new FormatException($"Invalid batch time format: {batchTime}");
		}
	}
}
