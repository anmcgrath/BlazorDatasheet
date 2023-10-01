using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Commands;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowAtCommand : IUndoableCommand
{
    private readonly int _index;
    private IReadOnlyList<CellMerge> _mergesPerformed = default!;
    private IReadOnlyList<CellMerge> _overridenMergedRegions = default!;

    /// <summary>
    /// Command for inserting a row into the sheet.
    /// </summary>
    /// <param name="index">The index that the row will be inserted at.</param>
    public InsertRowAtCommand(int index)
    {
        _index = index;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.InsertRowAtImpl(_index);
        (_mergesPerformed, _overridenMergedRegions) = sheet.Merges.RerangeMergedCells(Axis.Row, _index, 1);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.RemoveRowAtImpl(_index);
        sheet.Merges.UndoRerangeMergedCells(_mergesPerformed, _overridenMergedRegions);
        return true;
    }
}