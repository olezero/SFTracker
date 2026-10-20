using System.Globalization;

namespace SFTracker.Constants;

/// <summary>
/// Number formatting shared by the UI.
/// </summary>
public static class DisplayConstants {
	/// <summary>Format for the Resource Balance table (fixed two decimals).</summary>
	public const string DecimalFormat = "F2";

	/// <summary>Balances within ± this are shown as balanced (zero).</summary>
	public const double BalanceThreshold = 0.01;

	/// <summary>
	/// Compact format for rates and multipliers in recipe rows and editors: up to 4 decimals, trailing
	/// zeros trimmed. Invariant culture because the result is also fed back into number inputs.
	/// </summary>
	public static string FormatNumber(double value) =>
		Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
}
