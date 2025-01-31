using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.FormulaEngine;

internal class SetParsedFormulaCommand : BaseCommand, IUndoableCommand
{
    public int Row { get; }
    public int Column { get; }
    public CellFormula? CellFormula { get; }
    private FormulaVertex? _removedVertex;
    private FormulaVertex? _newVertex;

    public SetParsedFormulaCommand(int row, int column, CellFormula? cellFormula)
    {
        Row = row;
        Column = column;
        CellFormula = cellFormula;
    }

    public override bool Execute(Sheet sheet)
    {
        _removedVertex = sheet.FormulaEngine.GetVertex(Row, Column);
        sheet.FormulaEngine.SetFormula(Row, Column, CellFormula);
        _newVertex = sheet.FormulaEngine.GetVertex(Row, Column);
        return true;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public bool Undo(Sheet sheet)
    {
        if (_newVertex != null)
            sheet.FormulaEngine.RemoveFormulaVertex(_newVertex);

        if (_removedVertex != null)
            sheet.FormulaEngine.AddFormulaVertex(_removedVertex);
        return false;
    }
}