using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Selecting;

internal class SelectionInputManager : IDisposable
{
    private readonly Selection _selection;
    private int? _tabOriginColumn;
    private bool _handlingNavigation;
    public Selection Selection => _selection;

    public SelectionInputManager(Selection selection)
    {
        _selection = selection;
        _selection.SelectionChanged += ClearTabOriginOnSelectionChange;
        _selection.ActiveCellPositionChanged += ClearTabOriginOnPositionChange;
    }

    private void ClearTabOriginOnSelectionChange(object? sender, SelectionChangedEventArgs args)
    {
        if (!_handlingNavigation)
            _tabOriginColumn = null;
    }

    private void ClearTabOriginOnPositionChange(object? sender, ActiveCellPositionChangedEventArgs args)
    {
        if (!_handlingNavigation)
            _tabOriginColumn = null;
    }

    public void Dispose()
    {
        _selection.SelectionChanged -= ClearTabOriginOnSelectionChange;
        _selection.ActiveCellPositionChanged -= ClearTabOriginOnPositionChange;
    }

    public void HandleArrowKeyDown(bool shift, Offset offset)
    {
        _tabOriginColumn = null;
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
        var wasSingleCell = _selection.Regions.Count == 1 && _selection.ActiveRegion.IsSingleCell();
        var before = _selection.ActiveCellPosition;
        var origin = axis == Axis.Row && wasSingleCell &&
                     _tabOriginColumn is int col && _selection.Sheet.Columns.IsVisible(col)
            ? col
            : (int?)null;

        _handlingNavigation = true;
        try
        {
            _selection.HandleInput(SelectionInputKind.TabEnterNavigation, selection =>
            {
                selection.MoveActivePosition(axis, amount);
                if (origin is int originCol && selection.ActiveCellPosition.row != before.row)
                    selection.Set(selection.ActiveCellPosition.row, originCol);
            });
        }
        finally
        {
            _handlingNavigation = false;
        }

        if (axis == Axis.Col && wasSingleCell && _selection.ActiveCellPosition != before)
            _tabOriginColumn ??= before.col;
        else if (axis == Axis.Row)
            _tabOriginColumn = null;
    }

    public void HandleHeaderSelection(IRegion region)
    {
        _tabOriginColumn = null;
        _selection.HandleInput(SelectionInputKind.HeaderSelection, selection => selection.Set(region));
    }

    public void HandlePointerDown(int row, int col, bool shift, bool ctrl, bool meta, int mouseButton)
    {
        _tabOriginColumn = null;
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
