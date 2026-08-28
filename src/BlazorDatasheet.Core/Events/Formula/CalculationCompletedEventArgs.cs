namespace BlazorDatasheet.Core.Events.Formula;

/// <summary>
/// Provides information about a completed formula recalculation pass.
/// </summary>
public class CalculationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Whether all formulas were requested for recalculation.
    /// </summary>
    public bool CalculateAll { get; }

    /// <summary>
    /// The number of formulas evaluated during this pass.
    /// </summary>
    public int FormulaCount { get; }

    /// <summary>
    /// The time spent recalculating and applying formula results.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// An unexpected exception that interrupted recalculation, or null when recalculation succeeded.
    /// Formula errors such as circular references are values and are not reported here.
    /// </summary>
    public Exception? Exception { get; }

    public bool Succeeded => Exception is null;

    public CalculationCompletedEventArgs(bool calculateAll, int formulaCount, TimeSpan elapsed,
        Exception? exception)
    {
        CalculateAll = calculateAll;
        FormulaCount = formulaCount;
        Elapsed = elapsed;
        Exception = exception;
    }
}
