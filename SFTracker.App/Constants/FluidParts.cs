namespace SFTracker.Constants;

/// <summary>
/// Parts that are piped (liquids and gases). game_data.json carries no form flag, so this is by name.
/// Packaged variants are solids and intentionally not listed.
/// </summary>
public static class FluidParts {
	private static readonly HashSet<string> s_names = new(StringComparer.OrdinalIgnoreCase) {
		"Water",
		"Crude Oil",
		"Heavy Oil Residue",
		"Fuel",
		"Liquid Biofuel",
		"Turbofuel",
		"Rocket Fuel",
		"Ionized Fuel",
		"Alumina Solution",
		"Sulfuric Acid",
		"Nitric Acid",
		"Nitrogen Gas",
		"Dissolved Silica",
		"Dark Matter Residue",
		"Excited Photonic Matter",
	};

	public static bool IsFluid(string? partName) => partName != null && s_names.Contains(partName);

	/// <summary>Divisors offered by the fluid-scaling mod (1 = vanilla).</summary>
	public static readonly int[] AllowedDivisors = [1, 2, 3, 5, 10];
}
