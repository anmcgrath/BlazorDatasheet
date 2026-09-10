namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

/// <summary>
/// Shifts references on <paramref name="SheetName"/> by the given offset at evaluation time,
/// so a formula written against one anchor cell can be evaluated as if it were written at another.
/// </summary>
public readonly record struct ReferenceOffset(int RowOffset, int ColOffset, string SheetName);

public class FormulaEvaluationOptions
{
    /// <summary>
    /// When true, the formula evaluator should not resolve dependencies. Instead,
    /// references are CellValue.References. Default is false.
    /// </summary>
    public bool DoNotResolveDependencies { get; }

    /// <summary>
    /// When set, references are shifted by this offset before being resolved.
    /// </summary>
    public ReferenceOffset? ReferenceOffset { get; }

    /// <summary>
    /// Provides options for the formula <see cref="Evaluator"/>
    /// </summary>
    /// <param name="doNotResolveDependencies"> When true, the formula evaluator should not resolve dependencies. Instead,references are CellValue.References. Default is false.</param>
    /// <param name="referenceOffset">When set, references are shifted by this offset before being resolved.</param>
    public FormulaEvaluationOptions(bool doNotResolveDependencies, ReferenceOffset? referenceOffset = null)
    {
        DoNotResolveDependencies = doNotResolveDependencies;
        ReferenceOffset = referenceOffset;
    }

    public static FormulaEvaluationOptions Default => new(false);
}
