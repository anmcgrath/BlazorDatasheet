using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetFormulaCommand : IUndoableCommand
{
    public int Row { get; }
    public int Col { get; }
    public string? Formula { get; }
    private string? _previousFormula;

    public SetFormulaCommand(int row, int col, string? formula)
    {
        Row = row;
        Col = col;
        Formula = formula;
    }

    public bool Execute(Sheet sheet)
    {
        var cell = sheet.GetCell(Row, Col);
        _previousFormula = cell.FormulaString;
        return sheet.SetFormulaImpl(Row, Col, Formula);
    }

    public bool Undo(Sheet sheet)
    {
        return sheet.SetFormulaImpl(Row, Col, _previousFormula);
    }
}