namespace SFTracker.Constants;

/// <summary>Which machines the game settings modal offers a speed factor for.</summary>
public static class MachineSpeedSettings {
	// Extractors, the space elevator and the odd generators aren't things you overclock by tier in
	// the machine-tier mods, so they'd only clutter the list. (FICSMAS recipes are filtered out of
	// the recipe list already.)
	private static readonly HashSet<string> s_hidden = new(StringComparer.OrdinalIgnoreCase) {
		"Alien Power Augmenter",
		"Geothermal Generator",
		"Miner",
		"Oil Extractor",
		"Resource Well Extractor",
		"Resource Well Pressurizer",
		"Space Elevator",
		"Water Extractor",
	};

	public static bool IsListed(string machine) => !string.IsNullOrWhiteSpace(machine) && !s_hidden.Contains(machine);
}
