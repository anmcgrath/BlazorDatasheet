using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

/// <summary>
/// The borders a cell draws. The right and bottom edges are shared with the next visible cell,
/// and are drawn here for both cells. The left and top edges are drawn only by a cell that has no
/// visible cell before it, because otherwise that cell draws them.
/// </summary>
internal readonly record struct CellBorders(Border? Left, Border? Top, Border? Right, Border? Bottom)
{
    public bool Any => Left != null || Top != null || Right != null || Bottom != null;
}

/// <summary>
/// Decides the border drawn on each edge of a cell from the two cells that share the edge. A
/// border from a conditional format is drawn over one from a cell's format, and where both
/// cells give the edge a border of the same kind, the cell after the edge decides.
/// </summary>
/// <remarks>
/// Conditional format results are kept for the life of the resolver, since a cell is asked about
/// once for itself and again by the cells before it. A resolver must not outlive the pass that
/// builds the cells.
/// </remarks>
internal sealed class CellBorderResolver
{
    private readonly Sheet _sheet;
    private readonly bool _hasConditionalFormats;
    private readonly bool _hasMerges;
    private Dictionary<CellPosition, CellFormat?>? _conditionalFormats;

    public CellBorderResolver(Sheet sheet)
    {
        _sheet = sheet;
        _hasConditionalFormats = sheet.ConditionalFormats.HasAppliedFormats;
        _hasMerges = sheet.Cells.AnyMerges();
    }

    /// <summary>
    /// The result of the conditional formats at the cell. It is shared, so it must not be modified.
    /// </summary>
    public CellFormat? GetConditionalFormat(int row, int col)
    {
        if (!_hasConditionalFormats)
            return null;

        _conditionalFormats ??= new Dictionary<CellPosition, CellFormat?>();
        var position = new CellPosition(row, col);
        if (!_conditionalFormats.TryGetValue(position, out var format))
            _conditionalFormats[position] = format = _sheet.ConditionalFormats.GetFormatResult(row, col);

        return format;
    }

    /// <summary>
    /// Resolves the borders drawn by the cell at <paramref name="row"/>, <paramref name="col"/>.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="merge">The merge that the cell is drawn as, if any.</param>
    /// <param name="format">The cell's format, without its conditional format.</param>
    /// <param name="conditionalFormat">The cell's conditional format result.</param>
    public CellBorders Resolve(int row, int col, IRegion? merge, IReadonlyCellFormat? format,
        IReadonlyCellFormat? conditionalFormat)
    {
        var nextCol = Next(_sheet.Columns, merge?.Right ?? col);
        var nextRow = Next(_sheet.Rows, merge?.Bottom ?? row);

        var right = Shared(row, nextCol, nameof(CellFormat.BorderLeft),
            conditionalFormat?.BorderRight, format?.BorderRight);
        var bottom = Shared(nextRow, col, nameof(CellFormat.BorderTop),
            conditionalFormat?.BorderBottom, format?.BorderBottom);

        var left = HasPrevious(_sheet.Columns, merge?.Left ?? col)
            ? null
            : conditionalFormat?.BorderLeft ?? format?.BorderLeft;
        var top = HasPrevious(_sheet.Rows, merge?.Top ?? row)
            ? null
            : conditionalFormat?.BorderTop ?? format?.BorderTop;

        return new CellBorders(left, top, right, bottom);
    }

    private Border? Shared(int neighbourRow, int neighbourCol, string neighbourSide, Border? conditional,
        Border? stored)
    {
        if (neighbourRow < 0 || neighbourCol < 0)
            return conditional ?? stored;

        // a merge is drawn with the format of its first cell.
        if (_hasMerges && _sheet.Cells.GetMerge(neighbourRow, neighbourCol) is { } merge)
        {
            neighbourRow = Math.Max(merge.Top, 0);
            neighbourCol = Math.Max(merge.Left, 0);
        }

        return GetConditionalFormat(neighbourRow, neighbourCol)?.GetBorder(neighbourSide)
               ?? conditional
               ?? _sheet.GetBorderForRendering(neighbourRow, neighbourCol, neighbourSide)
               ?? stored;
    }

    // Almost always the next row or column is the visible one, which is much the cheaper question.
    private static int Next(RowColInfoStore store, int index) =>
        store.IsVisible(index + 1) ? index + 1 : store.GetNextVisible(index);

    private static bool HasPrevious(RowColInfoStore store, int index) =>
        index > 0 && (store.IsVisible(index - 1) || store.GetNextVisible(index, -1) >= 0);
}
