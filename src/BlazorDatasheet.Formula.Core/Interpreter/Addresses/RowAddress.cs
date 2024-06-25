namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class RowAddress : Address
{
    public int RowIndex { get; }
    public int Row { get; }
    public bool IsFixed { get; }

    public RowAddress(int rowIndex, int row, bool isFixed) : base(AddressKind.RowAddress)
    {
        RowIndex = rowIndex;
        Row = row;
        IsFixed = isFixed;
    }
}