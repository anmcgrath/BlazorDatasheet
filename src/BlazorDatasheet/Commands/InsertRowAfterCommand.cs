using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Commands;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowAfterCommand : IUndoableCommand
{
    private readonly int _index;
    private IReadOnlyList<CellMerge> _mergesPerformed = default!;
    private IReadOnlyList<CellMerge> _overridenMergedRegions = default!;

    /// <summary>
    /// Command for inserting a row into the sheet.
    /// </summary>
    /// <param name="index">The index that the row will be inserted AFTER.</param>
    public InsertRowAfterCommand(int index)
    {
        _index = index;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.InsertRowAfterImpl(_index);
        (_mergesPerformed, _overridenMergedRegions) = sheet.RerangeMergedCells(Axis.Row, _index, 1);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.RemoveRowAtImpl(_index + 1);
        sheet.UndoRerangeMergedCells(_mergesPerformed, _overridenMergedRegions);
        return true;
    }
}