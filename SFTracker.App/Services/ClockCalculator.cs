namespace SFTracker.Services;

/// <summary>
/// Helper maths for the "N machines at X%" calculator in the recipe editor. It never touches the
/// data: a factory recipe stores machine-equivalents (the multiplier), and this only shows one way
/// to split that across real machines with Satisfactory's clock speed (1%–250%, 4 decimals).
/// </summary>
public static class ClockCalculator {
	public const double MinClockPercent = 1;
	public const double MaxClockPercent = 250;

	/// <summary>
	/// Default machine count for a multiplier: the multiplier itself when it is whole, otherwise the
	/// next "nice" count at or above it, nice meaning a power of two or three times one (2, 3, 4, 6,
	/// 8, 12, 16, 24, 32, 48, …), since those lay out evenly. E.g. 21.5 → 24, 5.2 → 6, 22 → 22.
	/// </summary>
	public static int SuggestMachineCount(double multiplier) {
		if (multiplier <= 1) {
			return 1;
		}
		var whole = (int)Math.Round(multiplier);
		if (Math.Abs(multiplier - whole) < 1e-9) {
			return whole;
		}
		for (var n = (int)Math.Ceiling(multiplier); ; n++) {
			if (IsNice(n)) {
				return n;
			}
		}
	}

	private static bool IsNice(int n) {
		while (n % 2 == 0) {
			n /= 2;
		}
		if (n % 3 == 0) {
			n /= 3;
		}
		return n == 1;
	}

	/// <summary>
	/// Clock speed each of <paramref name="machines"/> machines needs to add up to the multiplier,
	/// rounded *up* to the game's 4 decimals so the machines never fall short.
	/// </summary>
	public static double ClockPercent(double multiplier, double machines) {
		var exact = multiplier / machines * 100;
		// Round away float noise first (2.4 / 3 * 100 is 80.00000000000001) so it isn't bumped to 80.0001.
		return Math.Ceiling(Math.Round(exact * 10000, 6)) / 10000;
	}

	/// <summary>Machines needed when each runs at the given clock speed.</summary>
	public static double MachinesAt(double multiplier, double clockPercent) => multiplier / clockPercent * 100;

	public static bool IsValidClock(double clockPercent) => clockPercent >= MinClockPercent && clockPercent <= MaxClockPercent;
}
