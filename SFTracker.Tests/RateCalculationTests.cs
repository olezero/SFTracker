namespace SFTracker.Tests;

/// <summary>DbRecipeInput/Output.GetQuantityPerMinute: the source of every per-minute number in the UI.</summary>
public class RateCalculationTests {
	private static readonly DbPart IronOre = Build.Part("Iron Ore");
	private static readonly DbPart IronIngot = Build.Part("Iron Ingot");
	private static readonly DbPart Bauxite = Build.Part("Bauxite");
	private static readonly DbPart Water = Build.Part("Water");
	private static readonly DbPart AluminaSolution = Build.Part("Alumina Solution");

	// 1 ore -> 1 ingot every 2 seconds = 30/min per machine
	private static readonly DbRecipe IronIngotRecipe = Build.Recipe("Iron Ingot", 2, [(IronOre, 1)], [(IronIngot, 1)]);
	// 12 bauxite + 18 water -> 12 alumina solution every 6 seconds = 120 + 180 -> 120 per minute
	private static readonly DbRecipe AluminaRecipe = Build.Recipe("Alumina Solution", 6, [(Bauxite, 12), (Water, 18)], [(AluminaSolution, 12)]);

	[Test]
	public void Rate_is_quantity_per_minute_times_multiplier() {
		var input = IronIngotRecipe.Inputs.Single();
		var output = IronIngotRecipe.Outputs.Single();

		Assert.Multiple(() => {
			Assert.That(input.GetQuantityPerMinute(1), Is.EqualTo(30));
			Assert.That(input.GetQuantityPerMinute(2.5), Is.EqualTo(75));
			Assert.That(output.GetQuantityPerMinute(2.5), Is.EqualTo(75));
		});
	}

	[Test]
	public void Sloop_doubles_output_but_not_input() {
		var factoryRecipe = Build.FactoryRecipe(IronIngotRecipe, 2, isSlooped: true);

		Assert.Multiple(() => {
			Assert.That(IronIngotRecipe.Inputs.Single().GetQuantityPerMinute(factoryRecipe), Is.EqualTo(60));
			Assert.That(IronIngotRecipe.Outputs.Single().GetQuantityPerMinute(factoryRecipe), Is.EqualTo(120));
			Assert.That(IronIngotRecipe.Outputs.Single().GetQuantityPerMinute(2, isSlooped: true), Is.EqualTo(120));
		});
	}

	[Test]
	public void Fluid_divisor_scales_fluids_only() {
		var bauxite = AluminaRecipe.Inputs.Single(i => i.Part == Bauxite);
		var water = AluminaRecipe.Inputs.Single(i => i.Part == Water);
		var alumina = AluminaRecipe.Outputs.Single();

		Assert.Multiple(() => {
			Assert.That(bauxite.GetQuantityPerMinute(1, fluidDivisor: 10), Is.EqualTo(120));
			Assert.That(water.GetQuantityPerMinute(1, fluidDivisor: 10), Is.EqualTo(18));
			Assert.That(alumina.GetQuantityPerMinute(1, fluidDivisor: 10), Is.EqualTo(12));
			Assert.That(alumina.GetQuantityPerMinute(1, isSlooped: true, fluidDivisor: 10), Is.EqualTo(24));
		});
	}

	[Test]
	public void Non_positive_fluid_divisor_is_treated_as_vanilla() {
		var water = AluminaRecipe.Inputs.Single(i => i.Part == Water);

		Assert.Multiple(() => {
			Assert.That(water.GetQuantityPerMinute(1, fluidDivisor: 0), Is.EqualTo(180));
			Assert.That(water.GetQuantityPerMinute(1, fluidDivisor: -3), Is.EqualTo(180));
		});
	}

	[Test]
	public void Fluid_parts_are_recognised_by_name_case_insensitively() {
		Assert.Multiple(() => {
			Assert.That(Water.IsFluid, Is.True);
			Assert.That(Build.Part("nitrogen gas").IsFluid, Is.True);
			Assert.That(Build.Part("Packaged Water").IsFluid, Is.False);
			Assert.That(IronOre.IsFluid, Is.False);
		});
	}
}
