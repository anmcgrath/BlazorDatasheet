using BlazorDatasheet.Formula.Core;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.Events.Edit;

public class EditAcceptedEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public CellValue Value { get; }

    public EditAcceptedEventArgs(int row, int col, CellValue value)
    {
        Row = row;
        Col = col;
        Value = value;
    }
}