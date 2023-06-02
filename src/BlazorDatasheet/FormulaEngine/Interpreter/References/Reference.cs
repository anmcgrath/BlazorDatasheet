namespace BlazorDatasheet.FormulaEngine.Interpreter.References;

public abstract class Reference
{
    /// <summary>
    /// The reference kind, e.g Row, Column, Cell, etc.
    /// </summary>
    public abstract ReferenceKind Kind { get; }
    /// <summary>
    /// Returns the reference as a string, in the correct reference order.
    /// </summary>
    /// <returns></returns>
    public abstract string ToRefText();
    /// <summary>
    /// Returns true if the positions + area of the reference are the same
    /// </summary>
    /// <param name="reference"></param>
    /// <returns></returns>
    public abstract bool SameAs(Reference reference);
    /// <summary>
    /// Whether the reference is relative to another cell. Rows/col will then be the number of rows/col away
    /// from where it is defined, e.g -1, 1 or 10, -5 etc.
    /// </summary>
    public bool IsRelativeReference { get; set; }

}