namespace SFTracker.Constants;

/// <summary>Position and size of one dashboard card in grid units (12 columns, rows of DashboardLayout.CellHeight px).</summary>
public record CardLayout(int X, int Y, int W, int H);

/// <summary>
/// The three dashboard cards and where they go until the user drags them elsewhere. Saved layouts
/// live in usersettings.json (UserSettings.Layout), keyed by these ids.
/// </summary>
public static class DashboardLayout {
	public const string Factories = "factories";
	public const string Balance = "balance";
	public const string Detail = "detail";

	public const int Columns = 12;
	public const int CellHeight = 40;

	// Sized for a 1080p desktop: narrow left column with the list above the balance, wide detail on the right.
	public static readonly IReadOnlyDictionary<string, CardLayout> Defaults = new Dictionary<string, CardLayout> {
		[Factories] = new(0, 0, 3, 9),
		[Balance] = new(0, 9, 3, 12),
		[Detail] = new(3, 0, 9, 21)
	};

	/// <summary>The saved layout for a card, or the default when none is saved.</summary>
	public static CardLayout Resolve(IReadOnlyDictionary<string, CardLayout>? saved, string id) =>
		saved != null && saved.TryGetValue(id, out var layout) ? layout : Defaults[id];
}
