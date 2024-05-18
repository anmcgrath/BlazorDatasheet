using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Layout;

/// <summary>
/// Stores a cache of sheet cell's that are within the render viewport.
/// </summary>
public class VisualSheet
{
    private readonly Sheet _sheet;
    private readonly Dictionary<CellPosition, VisualCell> _visualCache = new();
    private CellFormat _defaultFormat = new CellFormat();
    public Viewport Viewport { get; private set; } = new();
    
    /// <summary>
    /// The "visible" bounds of the sheet, shown in the scroll container
    /// </summary>
    public Rect ContainerBounds { get; private set; }

    public event EventHandler<VisualSheetInvalidateArgs>? Invalidated;

    public VisualSheet(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.SheetDirty += SheetOnSheetDirty;
    }

    private void SheetOnSheetDirty(object? sender, DirtySheetEventArgs e)
    {
        var set = new HashSet<CellPosition>();
        if (e.DirtyPositions != null)
        {
            InvalidateCells(e.DirtyPositions);
            set = e.DirtyPositions;
        }

        if (e.DirtyRegions != null)
        {
            InvalidateRegions(e.DirtyRegions);
            var invalidPositions = e.DirtyRegions.Select(x => x.GetIntersection(Viewport.VisibleRegion))
                .Where(x => x != null)
                .SelectMany(x => _sheet.Range(x).Positions);

            foreach (var position in invalidPositions)
                set.Add(position);
        }

        Invalidated?.Invoke(this, new VisualSheetInvalidateArgs(set));
    }

    public void UpdateViewport(Viewport newViewport)
    {
        var oldRegions = Viewport.VisibleRegion.Break(newViewport.VisibleRegion);
        var newRegions = newViewport.VisibleRegion.Break(Viewport.VisibleRegion);

        Viewport = newViewport;
        // Clear where we don't need to store anymore.
        RemoveRegionsFromCache(oldRegions);
        // Store Visual Cells from the new regions that we just encountered.
        InvalidateRegions(newRegions);

        Invalidated?.Invoke(this,
            new VisualSheetInvalidateArgs(newRegions.SelectMany(x => _sheet.Range(x).Positions).ToHashSet()));
    }

    public void InvalidateRegion(IRegion region)
    {
        InvalidateRegions(new[] { region });
    }

    /// <summary>
    /// Remove the cells within the regions from the cache.
    /// </summary>
    /// <param name="regions"></param>
    private void RemoveRegionsFromCache(IEnumerable<IRegion> regions)
    {
        var positions = regions.SelectMany(x => _sheet.Range(x).Positions);
        foreach (var cellPosition in positions)
        {
            if (_visualCache.ContainsKey(cellPosition))
                _visualCache.Remove(cellPosition);
        }
    }

    private void InvalidateRegions(IEnumerable<IRegion> regions)
    {
        if (Viewport == null) // only happens when we are starting up
            InvalidateCells(regions.SelectMany(x => _sheet.Range(x).Positions));
        else
        {
            var regionsInViewport =
                regions.Select(x => x.GetIntersection(Viewport.VisibleRegion))
                    .Where(x => x != null);

            var cells = regionsInViewport.SelectMany(x => _sheet.Range(x).Positions);
            InvalidateCells(cells);
        }
    }

    private void InvalidateCells(IEnumerable<CellPosition> cellPositions)
    {
        foreach (var cellPosition in cellPositions)
            InvalidateCell(cellPosition.row, cellPosition.col);
    }

    /// <summary>
    /// Invalidates a cell and updates the visual cell cache.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    private void InvalidateCell(int row, int col)
    {
        var visualCell = new VisualCell(row, col, _sheet);
        if (!_visualCache.TryAdd(new CellPosition(row, col), visualCell))
            _visualCache[new CellPosition(row, col)] = visualCell;
    }

    /// <summary>
    /// Returns the visual cell in the cache, at the position specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public VisualCell GetVisualCell(int row, int col)
    {
        if (_visualCache.TryGetValue(new CellPosition(row, col), out var cell))
            return cell;
        return VisualCell.Empty(row, col, _sheet, ref _defaultFormat);
    }
}