namespace SFTracker.Constants;

/// <summary>
/// Global constants for display formatting, particularly for decimal precision
/// </summary>
public static class DisplayConstants
{
    /// <summary>
    /// Maximum number of decimal places to display in the UI
    /// </summary>
    public const int DecimalPlaces = 2;
    
    /// <summary>
    /// Standard decimal format string for UI display
    /// </summary>
    public const string DecimalFormat = "F2";
    
    /// <summary>
    /// Precision threshold for determining if a balance is considered "zero"
    /// </summary>
    public const double BalanceThreshold = 0.01;
}