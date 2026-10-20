namespace SFTracker.Tests;

public class BalanceCalculatorTests {
	private static readonly DbPart IronOre = Build.Part("Iron Ore");
	private static readonly DbPart IronIngot = Build.Part("Iron Ingot");
	private static readonly DbPart Water = Build.Part("Water");

	// 30 ore/min -> 30 ingot/min per machine
	private static readonly DbRecipe IronIngotRecipe = Build.Recipe("Iron Ingot", 2, [(IronOre, 1)], [(IronIngot, 1)]);
	// A made-up recipe using water, to exercise the fluid divisor: 60 water/min -> 60 ingot/min per machine
	private static readonly DbRecipe WetIngotRecipe = Build.Recipe("Wet Iron Ingot", 1, [(IronOre, 1), (Water, 1)], [(IronIngot, 1)]);

	private static PartBalance BalanceOf(List<PartBalance> balances, DbPart part) => balances.Single(b => b.Part == part);

	[Test]
	public void Manual_inputs_consume_and_manual_outputs_produce() {
		var factory = Build.Factory();
		factory.Inputs.Add(Build.Input(IronOre, 100));
		factory.Outputs.Add(Build.Output(IronIngot, 10));

		var balances = BalanceCalculator.Calculate([factory]);

		Assert.Multiple(() => {
			Assert.That(balances, Has.Count.EqualTo(2));
			Assert.That(BalanceOf(balances, IronOre).Consumption, Is.EqualTo(100));
			Assert.That(BalanceOf(balances, IronOre).Balance, Is.EqualTo(-100));
			Assert.That(BalanceOf(balances, IronIngot).Production, Is.EqualTo(10));
			Assert.That(BalanceOf(balances, IronIngot).Balance, Is.EqualTo(10));
		});
	}

	[Test]
	public void Recipe_rates_are_added_on_top_of_manual_amounts() {
		var factory = Build.Factory();
		factory.Inputs.Add(Build.Input(IronOre, 100));
		factory.Outputs.Add(Build.Output(IronIngot, 10));
		factory.Recipes.Add(Build.FactoryRecipe(IronIngotRecipe, 2)); // 60 ore -> 60 ingot

		var balances = BalanceCalculator.Calculate([factory]);

		Assert.Multiple(() => {
			Assert.That(BalanceOf(balances, IronOre).Consumption, Is.EqualTo(160));
			Assert.That(BalanceOf(balances, IronOre).Production, Is.EqualTo(0));
			Assert.That(BalanceOf(balances, IronIngot).Production, Is.EqualTo(70));
			Assert.That(BalanceOf(balances, IronIngot).Consumption, Is.EqualTo(0));
		});
	}

	[Test]
	public void Disabled_overrides_are_skipped_and_parts_only_they_reference_are_omitted() {
		var factory = Build.Factory();
		var factoryRecipe = Build.FactoryRecipe(IronIngotRecipe, 2);
		factoryRecipe.InputOverrides.Add(new FactoryRecipeInputOverride { RecipeInputId = IronIngotRecipe.Inputs.Single().Id, IsEnabled = false });
		factory.Recipes.Add(factoryRecipe);

		var balances = BalanceCalculator.Calculate([factory]);

		Assert.Multiple(() => {
			Assert.That(balances.Select(b => b.Part), Is.EqualTo(new[] { IronIngot }));
			Assert.That(BalanceOf(balances, IronIngot).Production, Is.EqualTo(60));
		});
	}

	[Test]
	public void Inactive_recipes_are_ignored() {
		var factory = Build.Factory();
		var factoryRecipe = Build.FactoryRecipe(IronIngotRecipe, 2);
		factoryRecipe.IsActive = false;
		factory.Recipes.Add(factoryRecipe);

		Assert.That(BalanceCalculator.Calculate([factory]), Is.Empty);
	}

	[Test]
	public void Several_factories_are_summed_as_one_unit() {
		var smelter = Build.Factory("Smelter");
		smelter.Recipes.Add(Build.FactoryRecipe(IronIngotRecipe, 2)); // -60 ore, +60 ingot
		var consumer = Build.Factory("Consumer");
		consumer.Inputs.Add(Build.Input(IronIngot, 45));
		var miner = Build.Factory("Miner");
		miner.Outputs.Add(Build.Output(IronOre, 60));

		var balances = BalanceCalculator.Calculate([smelter, consumer, miner]);

		Assert.Multiple(() => {
			Assert.That(BalanceOf(balances, IronOre).Balance, Is.EqualTo(0));
			Assert.That(BalanceOf(balances, IronIngot).Balance, Is.EqualTo(15));
		});
	}

	[Test]
	public void Sloop_and_fluid_divisor_are_applied() {
		var factory = Build.Factory();
		factory.Recipes.Add(Build.FactoryRecipe(WetIngotRecipe, 1, isSlooped: true));

		var balances = BalanceCalculator.Calculate([factory], fluidDivisor: 10);

		Assert.Multiple(() => {
			Assert.That(BalanceOf(balances, IronOre).Consumption, Is.EqualTo(60));
			Assert.That(BalanceOf(balances, Water).Consumption, Is.EqualTo(6));
			Assert.That(BalanceOf(balances, IronIngot).Production, Is.EqualTo(120));
		});
	}
}
