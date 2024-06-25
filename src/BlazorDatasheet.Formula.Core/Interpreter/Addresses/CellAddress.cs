namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class CellAddress : Address
{
    public ColAddress ColAddress { get; }
    public RowAddress RowAddress { get; }

    public CellAddress(ColAddress colAddress, RowAddress rowAddress) : base(AddressKind.CellAddress)
    {
        ColAddress = colAddress;
        RowAddress = rowAddress;
    }
}