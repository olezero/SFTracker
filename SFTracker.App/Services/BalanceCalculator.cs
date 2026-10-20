using SFTracker.Models;

namespace SFTracker.Services;

/// <summary>Production and consumption of one part across a set of factories.</summary>
public class PartBalance {
	public required DbPart Part { get; init; }
	public double Production { get; set; }
	public double Consumption { get; set; }
	public double Balance => Production - Consumption;
}

/// <summary>
/// The resource balance: manual inputs/outputs plus the enabled inputs/outputs of every active recipe,
/// summed per part. Factory inputs and recipe inputs count as consumption; outputs as production.
/// Pure and in-memory; the factories must be loaded with their parts, recipes and overrides
/// (see FactoryService.GetAllFactoriesAsync).
/// </summary>
public static class BalanceCalculator {
	public static List<PartBalance> Calculate(IEnumerable<Factory> factories, int fluidDivisor = 1) {
		var byPartId = new Dictionary<int, PartBalance>();
		PartBalance For(DbPart part) {
			if (!byPartId.TryGetValue(part.Id, out var balance)) {
				balance = byPartId[part.Id] = new PartBalance { Part = part };
			}
			return balance;
		}

		foreach (var factory in factories) {
			foreach (var input in factory.Inputs) {
				For(input.Part).Consumption += input.AmountPerMinute;
			}
			foreach (var output in factory.Outputs) {
				For(output.Part).Production += output.AmountPerMinute;
			}
			foreach (var factoryRecipe in factory.Recipes.Where(r => r.IsActive)) {
				foreach (var input in factoryRecipe.Recipe.Inputs.Where(i => factoryRecipe.IsInputEnabled(i.Id))) {
					For(input.Part).Consumption += input.GetQuantityPerMinute(factoryRecipe, fluidDivisor);
				}
				foreach (var output in factoryRecipe.Recipe.Outputs.Where(o => factoryRecipe.IsOutputEnabled(o.Id))) {
					For(output.Part).Production += output.GetQuantityPerMinute(factoryRecipe, fluidDivisor);
				}
			}
		}

		return byPartId.Values
			.Where(b => b.Production != 0 || b.Consumption != 0)
			.ToList();
	}
}
