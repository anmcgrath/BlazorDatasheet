namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public readonly record struct FormulaCallerInfo(int RowIndex, int ColIndex, string SheetName)
{
}
