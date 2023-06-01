namespace BlazorDatasheet.FormulaEngine.Interpreter.References;

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