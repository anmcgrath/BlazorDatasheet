using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.FormulaEngine.Interpreter.References;

namespace ExpressionEvaluator.CodeAnalysis.Types;

public abstract class Reference
{
    public abstract ReferenceKind Kind { get; }
    public abstract string ToRefText();
    public abstract bool SameAs(Reference reference);
    /// <summary>
    /// Whether the reference is resolved
    /// </summary>
    public bool IsResolved { get; }

}