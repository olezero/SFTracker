using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SFTracker.Data;
using SFTracker.Models;

namespace SFTracker.Services;

/// <summary>
/// Exports a game to a self-contained JSON document and imports such a document as a new game.
/// Parts and recipes are referenced by name (ids differ between databases); only user data is
/// included, never the game reference data.
/// </summary>
public class GameTransferService {
	public const string Format = "sftracker-game";
	public const int Version = 1;

	private static readonly JsonSerializerOptions s_jsonOptions = new() {
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private readonly SFTrackerDbContext m_context;

	public GameTransferService(SFTrackerDbContext context) {
		m_context = context;
	}

	#region JSON document shape

	public class GameExport {
		public string Format { get; set; } = GameTransferService.Format;
		public int Version { get; set; } = GameTransferService.Version;
		public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
		public string Name { get; set; } = string.Empty;
		public int FluidDivisor { get; set; } = 1;
		public List<string> Networks { get; set; } = [];
		public List<FactoryExport> Factories { get; set; } = [];
	}

	public class FactoryExport {
		public string Name { get; set; } = string.Empty;
		public double Sorting { get; set; }
		public string? Network { get; set; }
		public List<AmountExport> Inputs { get; set; } = [];
		public List<AmountExport> Outputs { get; set; } = [];
		public List<FactoryRecipeExport> Recipes { get; set; } = [];
	}

	public class AmountExport {
		public string Part { get; set; } = string.Empty;
		public double AmountPerMinute { get; set; }
	}

	public class FactoryRecipeExport {
		public string Recipe { get; set; } = string.Empty;
		public double Multiplier { get; set; }
		public bool IsSlooped { get; set; }
		public double Sorting { get; set; }
		/// <summary>Part names of recipe inputs toggled off in the balance (absent = enabled).</summary>
		public List<string> DisabledInputs { get; set; } = [];
		public List<string> DisabledOutputs { get; set; } = [];
	}

	#endregion

	public async Task<string> ExportAsync(int gameId) {
		var game = await m_context.Games.FindAsync(gameId)
			?? throw new InvalidOperationException($"Game {gameId} not found.");

		var networks = await m_context.Networks
			.Where(n => n.GameId == gameId)
			.OrderBy(n => n.Name)
			.ToListAsync();

		var factories = await m_context.Factories
			.Where(f => f.GameId == gameId)
			.Include(f => f.Network)
			.Include(f => f.Inputs).ThenInclude(i => i.Part)
			.Include(f => f.Outputs).ThenInclude(o => o.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.Recipe)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.InputOverrides).ThenInclude(o => o.RecipeInput).ThenInclude(ri => ri.Part)
			.Include(f => f.Recipes.Where(r => r.IsActive)).ThenInclude(r => r.OutputOverrides).ThenInclude(o => o.RecipeOutput).ThenInclude(ro => ro.Part)
			.OrderBy(f => f.Sorting).ThenBy(f => f.Id)
			.AsSplitQuery()
			.ToListAsync();

		var export = new GameExport {
			Name = game.Name,
			FluidDivisor = game.FluidDivisor,
			Networks = networks.Select(n => n.Name).ToList(),
			Factories = factories.Select(f => new FactoryExport {
				Name = f.Name,
				Sorting = f.Sorting,
				Network = f.Network?.Name,
				Inputs = f.Inputs.OrderBy(i => i.Part.Name).Select(i => new AmountExport { Part = i.Part.Name, AmountPerMinute = i.AmountPerMinute }).ToList(),
				Outputs = f.Outputs.OrderBy(o => o.Part.Name).Select(o => new AmountExport { Part = o.Part.Name, AmountPerMinute = o.AmountPerMinute }).ToList(),
				Recipes = f.Recipes.OrderBy(r => r.Sorting).ThenBy(r => r.Id).Select(r => new FactoryRecipeExport {
					Recipe = r.Recipe.Name,
					Multiplier = r.Multiplier,
					IsSlooped = r.IsSlooped,
					Sorting = r.Sorting,
					DisabledInputs = r.InputOverrides.Where(o => !o.IsEnabled).Select(o => o.RecipeInput.Part.Name).OrderBy(n => n).ToList(),
					DisabledOutputs = r.OutputOverrides.Where(o => !o.IsEnabled).Select(o => o.RecipeOutput.Part.Name).OrderBy(n => n).ToList()
				}).ToList()
			}).ToList()
		};

		return JsonSerializer.Serialize(export, s_jsonOptions);
	}

	/// <summary>
	/// Creates a new game from an exported document. Never touches existing games: the new game is
	/// named "&lt;name&gt; (imported)", with a counter if that is taken. Throws
	/// <see cref="GameImportException"/> with a readable message when the document is invalid or
	/// references parts/recipes this database doesn't know.
	/// </summary>
	public async Task<Game> ImportAsync(string json) {
		GameExport? export;
		try {
			export = JsonSerializer.Deserialize<GameExport>(json, s_jsonOptions);
		} catch (JsonException ex) {
			throw new GameImportException($"Not valid JSON: {ex.Message}");
		}
		if (export == null || export.Format != Format) {
			throw new GameImportException($"Not an SFTracker game export (expected \"format\": \"{Format}\").");
		}

		if (export.Version > Version) {
			throw new GameImportException($"Export version {export.Version} is newer than this app supports ({Version}).");
		}

		if (string.IsNullOrWhiteSpace(export.Name)) {
			throw new GameImportException("The export has no game name.");
		}

		// Resolve every referenced part/recipe up front so a bad document fails before anything is written.
		var partsByName = await m_context.Parts.ToDictionaryAsync(p => p.Name);
		var recipesByName = await m_context.Recipes
			.Include(r => r.Inputs).ThenInclude(i => i.Part)
			.Include(r => r.Outputs).ThenInclude(o => o.Part)
			.AsSplitQuery()
			.ToDictionaryAsync(r => r.Name);

		var missing = new SortedSet<string>();
		foreach (var f in export.Factories) {
			foreach (var a in f.Inputs.Concat(f.Outputs)) {
				if (!partsByName.ContainsKey(a.Part)) {
					missing.Add($"part \"{a.Part}\"");
				}
			}

			foreach (var r in f.Recipes) {
				if (!recipesByName.TryGetValue(r.Recipe, out var recipe)) {
					missing.Add($"recipe \"{r.Recipe}\"");
					continue;
				}
				foreach (var name in r.DisabledInputs) {
					if (!recipe.Inputs.Any(i => i.Part.Name == name)) {
						missing.Add($"input \"{name}\" of recipe \"{r.Recipe}\"");
					}
				}

				foreach (var name in r.DisabledOutputs) {
					if (!recipe.Outputs.Any(o => o.Part.Name == name)) {
						missing.Add($"output \"{name}\" of recipe \"{r.Recipe}\"");
					}
				}
			}
			if (f.Network != null && !export.Networks.Contains(f.Network)) {
				export.Networks.Add(f.Network);
			}
		}
		if (missing.Count > 0) {
			throw new GameImportException("Unknown in this database: " + string.Join(", ", missing));
		}

		var fluidDivisor = Constants.FluidParts.AllowedDivisors.Contains(export.FluidDivisor) ? export.FluidDivisor : 1;
		var game = new Game {
			Name = await UniqueGameNameAsync($"{export.Name.Trim()} (imported)"),
			FluidDivisor = fluidDivisor
		};

		var networksByName = export.Networks.Distinct().ToDictionary(n => n, n => new Network { Name = n, Game = game });
		foreach (var network in networksByName.Values) {
			game.Networks.Add(network);
		}

		foreach (var f in export.Factories) {
			var factory = new Factory {
				Name = f.Name,
				Sorting = f.Sorting,
				Game = game,
				Network = f.Network != null ? networksByName[f.Network] : null
			};
			foreach (var a in f.Inputs) {
				factory.Inputs.Add(new FactoryInput { PartId = partsByName[a.Part].Id, AmountPerMinute = a.AmountPerMinute });
			}

			foreach (var a in f.Outputs) {
				factory.Outputs.Add(new FactoryOutput { PartId = partsByName[a.Part].Id, AmountPerMinute = a.AmountPerMinute });
			}

			foreach (var r in f.Recipes) {
				var recipe = recipesByName[r.Recipe];
				var factoryRecipe = new FactoryRecipe {
					RecipeId = recipe.Id,
					Multiplier = r.Multiplier,
					IsSlooped = r.IsSlooped,
					IsActive = true,
					Sorting = r.Sorting
				};
				foreach (var name in r.DisabledInputs.Distinct()) {
					factoryRecipe.InputOverrides.Add(new FactoryRecipeInputOverride {
						RecipeInputId = recipe.Inputs.First(i => i.Part.Name == name).Id,
						IsEnabled = false
					});
				}

				foreach (var name in r.DisabledOutputs.Distinct()) {
					factoryRecipe.OutputOverrides.Add(new FactoryRecipeOutputOverride {
						RecipeOutputId = recipe.Outputs.First(o => o.Part.Name == name).Id,
						IsEnabled = false
					});
				}

				factory.Recipes.Add(factoryRecipe);
			}
			game.Factories.Add(factory);
		}

		m_context.Games.Add(game);
		await m_context.SaveChangesAsync();
		return game;
	}

	private async Task<string> UniqueGameNameAsync(string baseName) {
		var existing = await m_context.Games.Select(g => g.Name).ToListAsync();
		var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
		var name = baseName;
		for (var i = 2; taken.Contains(name); i++) {
			name = $"{baseName} ({i})";
		}

		return name;
	}
}

public class GameImportException(string message) : Exception(message);
