using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

public class SelectionInputManager
{
    private readonly Selection _selection;
    public Selection Selection => _selection;

    public SelectionInputManager(Selection selection)
    {
        _selection = selection;
    }

    public void HandleArrowKeyDown(bool shift, Offset offset)
    {
        if (shift)
            _selection.GrowActiveSelection(offset);
        else
            CollapseAndMoveSelection(offset);
    }

    private void CollapseAndMoveSelection(Offset offset)
    {
        if (_selection.ActiveRegion == null)
            return;

        if (_selection.IsSelecting)
            return;

        var posn = _selection.ActiveCellPosition;

        _selection.Set(posn.row, posn.col);
        _selection.MoveActivePositionByRow(offset.Rows);
        _selection.MoveActivePositionByCol(offset.Columns);
    }

    public void HandlePointerDown(int row, int col, bool shift, bool ctrl, bool meta, int mouseButton)
    {
        if (shift && _selection.ActiveRegion != null)
        {
            _selection.ExtendTo(row, col);
        }
        else
        {
            if (!meta && !ctrl)
            {
                _selection.ClearSelections();
            }

            if (row == -1 && col == -1)
                return;
            else if (row == -1)
                _selection.BeginSelectingCol(col);
            else if (col == -1)
                _selection.BeginSelectingRow(row);
            else
                _selection.BeginSelectingCell(row, col);

            if (mouseButton == 2) // RMC
                _selection.EndSelecting();
        }
    }

    public void HandlePointerOver(int row, int col)
    {
        _selection.UpdateSelectingEndPosition(row, col);
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