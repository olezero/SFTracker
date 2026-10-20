namespace SFTracker.Tests;

/// <summary>Override semantics: absence of an override means enabled.</summary>
public class FactoryRecipeTests {
	[Test]
	public void Inputs_and_outputs_are_enabled_without_an_override() {
		var factoryRecipe = new FactoryRecipe();

		Assert.Multiple(() => {
			Assert.That(factoryRecipe.IsInputEnabled(1), Is.True);
			Assert.That(factoryRecipe.IsOutputEnabled(1), Is.True);
		});
	}

	[Test]
	public void Override_decides_when_present() {
		var factoryRecipe = new FactoryRecipe();
		factoryRecipe.InputOverrides.Add(new FactoryRecipeInputOverride { RecipeInputId = 1, IsEnabled = false });
		factoryRecipe.InputOverrides.Add(new FactoryRecipeInputOverride { RecipeInputId = 2, IsEnabled = true });
		factoryRecipe.OutputOverrides.Add(new FactoryRecipeOutputOverride { RecipeOutputId = 1, IsEnabled = false });

		Assert.Multiple(() => {
			Assert.That(factoryRecipe.IsInputEnabled(1), Is.False);
			Assert.That(factoryRecipe.IsInputEnabled(2), Is.True);
			Assert.That(factoryRecipe.IsInputEnabled(3), Is.True, "no override for id 3");
			Assert.That(factoryRecipe.IsOutputEnabled(1), Is.False);
			Assert.That(factoryRecipe.IsOutputEnabled(2), Is.True);
		});
	}
}
