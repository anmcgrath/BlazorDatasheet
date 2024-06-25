namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class RangeAddress :Address
{
    public Address Start { get; }
    public Address End { get; }

    public RangeAddress(Address start, Address end) : base(AddressKind.RangeAddress)
    {
        Start = start;
        End = end;
    }
}