using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public readonly record struct FormulaCallerInfo(int RowIndex, int ColIndex, string SheetName)
{
    public CellReference ToCellReference()
    {
        var cellReference = new CellReference(RowIndex, ColIndex, false, false);
        cellReference.SetSheetName(SheetName, false);
        return cellReference;
    }
}
