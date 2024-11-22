namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class FormulaEvaluationOptions
{
    /// <summary>
    /// When true, the formula evaluator should not resolve dependencies. Instead,
    /// references are CellValue.References. Default is false.
    /// </summary>
    public bool DoNotResolveDependencies { get; }

    /// <summary>
    /// Provides options for the formula <see cref="Evaluator"/>
    /// </summary>
    /// <param name="doNotResolveDependencies"> When true, the formula evaluator should not resolve dependencies. Instead,references are CellValue.References. Default is false.</param>
    public FormulaEvaluationOptions(bool doNotResolveDependencies)
    {
        DoNotResolveDependencies = doNotResolveDependencies;
    }

    public static FormulaEvaluationOptions Default => new(false);
}