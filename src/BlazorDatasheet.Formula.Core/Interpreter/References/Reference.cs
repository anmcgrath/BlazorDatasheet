using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

public abstract class Reference
{
    /// <summary>
    /// The reference kind, e.g Cell, Range, Named
    /// </summary>
    public abstract ReferenceKind Kind { get; }
    /// <summary>
    /// Returns the reference as a string, in the correct reference order.
    /// </summary>
    /// <returns></returns>
    public abstract string ToAddressText();
    /// <summary>
    /// Returns true if the positions + area of the reference are the same
    /// </summary>
    /// <param name="reference"></param>
    /// <returns></returns>
    public abstract bool SameAs(Reference reference);
    /// <summary>
    /// Shift this reference by the given offset.
    /// </summary>
    /// <param name="offsetRow"></param>
    /// <param name="offsetCol"></param>
    public abstract void Shift(int offsetRow, int offsetCol);
    /// <summary>
    /// Whether the reference is valid. If it is not, then the formula should evaluate to #REF.
    /// </summary>
    public abstract bool IsInvalid { get; protected set; }
    /// <summary>
    /// Returns a region that is the size of the reference.
    /// </summary>
    /// <returns></returns>
    public abstract IRegion Region { get; protected set; }
    internal abstract void SetRegion(IRegion region);
    internal void SetValidity(bool isValid)
    {
        IsInvalid = !isValid;
    }
}