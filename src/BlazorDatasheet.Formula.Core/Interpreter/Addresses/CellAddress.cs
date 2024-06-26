namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class CellAddress : Address
{
    public ColAddress ColAddress { get; }
    public RowAddress RowAddress { get; }

    public CellAddress(RowAddress rowAddress, ColAddress colAddress) : base(AddressKind.CellAddress)
    {
        ColAddress = colAddress;
        RowAddress = rowAddress;
    }

    /// <summary>
    /// Creates a cell address from the row/column index.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="colIndex"></param>
    internal CellAddress(int rowIndex, int colIndex) : base(AddressKind.CellAddress)
    {
        ColAddress = new ColAddress(colIndex, RangeText.ColNumberToLetters(colIndex), false);
        RowAddress = new RowAddress(rowIndex, rowIndex + 1, false);
    }
}