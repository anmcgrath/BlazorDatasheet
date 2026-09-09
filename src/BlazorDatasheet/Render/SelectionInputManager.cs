using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

internal class SelectionInputManager
{
    private readonly Selection _selection;
    public Selection Selection => _selection;

    public SelectionInputManager(Selection selection)
    {
        _selection = selection;
    }

    public void HandleArrowKeyDown(bool shift, Offset offset)
    {
        if (_selection.ActiveRegion == null || (!shift && _selection.IsSelecting))
            return;
        _selection.HandleInput(shift ? SelectionInputKind.ShiftExtension : SelectionInputKind.ArrowNavigation,
            selection =>
            {
                if (shift)
                    selection.GrowActiveSelection(offset);
                else
                    CollapseAndMoveSelection(selection, offset);
            });
    }

    private static void CollapseAndMoveSelection(Selection selection, Offset offset)
    {
        if (selection.ActiveRegion == null)
            return;

        if (selection.IsSelecting)
            return;

        var posn = selection.ActiveCellPosition;

        if (!selection.ActiveRegion.IsSingleCell())
            selection.Set(posn.row, posn.col);

        selection.MoveActivePositionByRow(offset.Rows);
        selection.MoveActivePositionByCol(offset.Columns);
    }

    public void HandleTabEnterNavigation(Axis axis, int amount)
    {
        if (_selection.ActiveRegion == null || amount == 0)
            return;
        _selection.HandleInput(SelectionInputKind.TabEnterNavigation,
            selection => selection.MoveActivePosition(axis, amount));
    }

    public void HandleHeaderSelection(IRegion region) =>
        _selection.HandleInput(SelectionInputKind.HeaderSelection, selection => selection.Set(region));

    public void HandlePointerDown(int row, int col, bool shift, bool ctrl, bool meta, int mouseButton)
    {
        var kind = shift && _selection.ActiveRegion != null ? SelectionInputKind.ShiftExtension :
            row == -1 || col == -1 ? SelectionInputKind.HeaderSelection : SelectionInputKind.PointerStart;
        _selection.HandleInput(kind,
            selection => HandlePointerDown(selection, row, col, shift, ctrl, meta, mouseButton));
    }

    private static void HandlePointerDown(Selection selection, int row, int col, bool shift, bool ctrl,
        bool meta, int mouseButton)
    {
        if (shift && selection.ActiveRegion != null)
        {
            selection.ExtendTo(row, col);
        }
        else
        {
            if (!meta && !ctrl)
            {
                selection.ClearSelections();
            }

            if (row == -1 && col == -1)
                return;
            else if (row == -1)
                selection.BeginSelectingCol(col);
            else if (col == -1)
                selection.BeginSelectingRow(row);
            else
                selection.BeginSelectingCell(row, col);

            if (mouseButton == 2) // RMC
                selection.EndSelecting();
        }
    }

    public void HandlePointerOver(int row, int col)
    {
        if (!_selection.IsSelecting)
            return;
        _selection.HandleInput(SelectionInputKind.Drag, selection => selection.UpdateSelectingEndPosition(row, col));
    }

    public void HandleWindowMouseUp()
    {
        _selection.EndSelecting();
    }

    public void Clear()
    {
        _selection.ClearSelections();
        _selection.EndSelecting();
    }
}
