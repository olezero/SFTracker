namespace SFTracker.Constants;

/// <summary>Position and size of one dashboard card in grid units (DashboardLayout.Columns columns, rows of DashboardLayout.CellHeight px).</summary>
public record CardLayout(int X, int Y, int W, int H);

/// <summary>
/// The three dashboard cards and where they go until the user drags them elsewhere. Saved layouts
/// live in usersettings.json (UserSettings.Layout), keyed by these ids.
/// </summary>
public static class DashboardLayout {
	public const string Factories = "factories";
	public const string Balance = "balance";
	public const string Detail = "detail";

	// 24 columns rather than Bootstrap's 12 so widths snap in finer steps; heights already do at 40px.
	public const int Columns = 24;
	public const int CellHeight = 40;

	// Sized for a 1080p desktop: narrow left column with the list above the balance, wide detail on the right.
	public static readonly IReadOnlyDictionary<string, CardLayout> Defaults = new Dictionary<string, CardLayout> {
		[Factories] = new(0, 0, 6, 9),
		[Balance] = new(0, 9, 6, 12),
		[Detail] = new(6, 0, 18, 21)
	};

	/// <summary>The saved layout for a card, or the default when none is saved.</summary>
	public static CardLayout Resolve(IReadOnlyDictionary<string, CardLayout>? saved, string id) =>
		saved != null && saved.TryGetValue(id, out var layout) ? layout : Defaults[id];

	/// <summary>
	/// Rescales a layout saved under a different column count to <see cref="Columns"/>, so a change
	/// to the grid resolution doesn't shift the saved cards. Rows are unaffected.
	/// </summary>
	public static Dictionary<string, CardLayout> Rescale(IReadOnlyDictionary<string, CardLayout> saved, int fromColumns) {
		var factor = (double)Columns / fromColumns;
		return saved.ToDictionary(kv => kv.Key, kv => kv.Value with {
			X = (int)Math.Round(kv.Value.X * factor),
			W = Math.Max(1, (int)Math.Round(kv.Value.W * factor))
		});
	}
}
