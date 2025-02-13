using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.FormulaEngine;

/// <summary>
/// Gets attached to the <see cref="InsertRowsColsCommand"/>
/// </summary>
internal class InsertRowColIntoFormulaCommand : BaseCommand, IUndoableCommand
{
    public int Index { get; }
    public int Count { get; }
    public Axis Axis { get; }

    public InsertRowColIntoFormulaCommand(int index, int count, Axis axis)
    {
        Index = index;
        Count = count;
        Axis = axis;
    }

    public override bool Execute(Sheet sheet)
    {
        sheet.FormulaEngine.InsertRowCol(Index, Count, Axis);
        return true;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public bool Undo(Sheet sheet)
    {
        sheet.FormulaEngine.RemoveRowCol(Index, Count, Axis);
        return true;
    }
}