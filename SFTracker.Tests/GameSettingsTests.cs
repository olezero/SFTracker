using System.Text.Json;

namespace SFTracker.Tests;

public class GameSettingsTests {
	[Test]
	public void Empty_or_broken_json_gives_defaults() {
		Assert.Multiple(() => {
			Assert.That(GameSettings.Parse(null).MachineSpeeds, Is.Empty);
			Assert.That(GameSettings.Parse("").MachineSpeeds, Is.Empty);
			Assert.That(GameSettings.Parse("{not json").MachineSpeeds, Is.Empty);
			Assert.That(GameSettings.Parse("{}").SpeedOf("Constructor"), Is.EqualTo(1));
		});
	}

	[Test]
	public void Round_trips_and_keeps_unknown_keys() {
		var settings = GameSettings.Parse("""{"machineSpeeds":{"Constructor":3,"Manufacturer":5},"futureThing":{"a":1}}""");

		var json = settings.ToJson();
		var again = GameSettings.Parse(json);

		Assert.Multiple(() => {
			Assert.That(settings.SpeedOf("Constructor"), Is.EqualTo(3));
			Assert.That(settings.SpeedOf("Smelter"), Is.EqualTo(1), "missing = 1x");
			Assert.That(again.MachineSpeeds, Is.EqualTo(settings.MachineSpeeds));
			Assert.That(JsonDocument.Parse(json).RootElement.GetProperty("futureThing").GetProperty("a").GetInt32(), Is.EqualTo(1));
		});
	}
}
