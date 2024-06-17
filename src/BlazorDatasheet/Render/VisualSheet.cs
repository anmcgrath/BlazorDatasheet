using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

/// <summary>
/// Stores a cache of sheet cell's that are within the render viewport.
/// </summary>
public class VisualSheet
{
    private readonly Sheet _sheet;
    private readonly Dictionary<CellPosition, VisualCell> _visualCache = new();
    private CellFormat _defaultFormat = new CellFormat();
    public Viewport Viewport { get; private set; } = new();
    public event EventHandler<VisualSheetInvalidateArgs>? Invalidated;

    public VisualSheet(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.SheetDirty += SheetOnSheetDirty;
    }

    private void SheetOnSheetDirty(object? sender, DirtySheetEventArgs e)
    {
        var dirtyRows = new HashSet<int>();

        var dirtyRegions = e.DirtyRegions
            .GetDataRegions(this.Viewport.VisibleRegion)
            .Select(x => x.Region)
            .Select(x => x.GetIntersection(Viewport.VisibleRegion))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        UpdateRegionCache(dirtyRegions);

        foreach (var region in dirtyRegions)
        {
            for (int i = region.Top; i <= region.Bottom; i++)
                dirtyRows.Add(i);
        }

        Invalidated?.Invoke(this, new VisualSheetInvalidateArgs(dirtyRows));
    }

    public void UpdateViewport(Viewport newViewport)
    {
        var oldRegions = Viewport.VisibleRegion.Break(newViewport.VisibleRegion);
        var newRegions = newViewport.VisibleRegion.Break(Viewport.VisibleRegion);

        Viewport = newViewport;
        // Clear where we don't need to store anymore.
        RemoveRegionsFromCache(oldRegions);
        // Store Visual Cells from the new regions that we just encountered.
        UpdateRegionCache(newRegions);

        var dirtyRows = newRegions
            .SelectMany(x => Enumerable.Range(x.Top, x.Height))
            .Concat(oldRegions.SelectMany(x => Enumerable.Range(x.Top, x.Height)))
            .ToHashSet();

        Invalidated?.Invoke(this,
            new VisualSheetInvalidateArgs(dirtyRows));
    }

    public void InvalidateRegion(IRegion region)
    {
        UpdateRegionCache(new[] { region });
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

    private void UpdateRegionCache(IEnumerable<IRegion> regions)
    {
        if (Viewport == null) // only happens when we are starting up
            UpdateCellCaches(regions.SelectMany(x => _sheet.Range(x).Positions));
        else
        {
            var regionsInViewport =
                regions.Select(x => x.GetIntersection(Viewport.VisibleRegion))
                    .Where(x => x != null);

            var cells = regionsInViewport.SelectMany(x => _sheet.Range(x).Positions);
            UpdateCellCaches(cells);
        }
    }

    private void UpdateCellCaches(IEnumerable<CellPosition> cellPositions)
    {
        foreach (var cellPosition in cellPositions)
            UpdateCellCache(cellPosition.row, cellPosition.col);
    }

    /// <summary>
    /// Updates the visual cell cache for a cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    private void UpdateCellCache(int row, int col)
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

        return new VisualCell(row, col, _sheet);
    }
}