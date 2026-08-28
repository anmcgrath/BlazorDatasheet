namespace BlazorDatasheet.Core.Events.Formula;

/// <summary>
/// Provides information about a formula recalculation pass that is about to run.
/// </summary>
public class CalculationStartedEventArgs : EventArgs
{
    /// <summary>
    /// Whether all formulas were requested for recalculation.
    /// </summary>
    public bool CalculateAll { get; }

    /// <summary>
    /// The number of formulas that will be evaluated during this pass.
    /// </summary>
    public int FormulaCount { get; }

    public CalculationStartedEventArgs(bool calculateAll, int formulaCount)
    {
        CalculateAll = calculateAll;
        FormulaCount = formulaCount;
    }
}
