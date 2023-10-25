using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

public class VisualSheet
{
    private readonly Sheet _sheet;
    private readonly Dictionary<(int row, int col), VisualCell> _visualCache = new();
    private CellFormat _defaultFormat = new CellFormat();
    private Viewport? _currentViewport = null;

    public event EventHandler<VisualSheetInvalidateArgs> Invalidated;

    public VisualSheet(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.SheetDirty += SheetOnSheetDirty;
    }

    private void SheetOnSheetDirty(object? sender, DirtySheetEventArgs e)
    {
        var set = new HashSet<(int row, int col)>();
        if (e.DirtyPositions != null)
        {
            InvalidateCells(e.DirtyPositions);
            set = e.DirtyPositions;
        }

        if (e.DirtyRegions != null)
        {
            InvalidateRegions(e.DirtyRegions);
            foreach (var position in _sheet.Range(e.DirtyRegions).Positions)
                set.Add(position);
        }

        Invalidated?.Invoke(this, new VisualSheetInvalidateArgs(set));
    }

    public void UpdateViewport(Sheet sheet, Viewport viewport)
    {
        if (_currentViewport == null)
        {
            InvalidateRegion(viewport.VisibleRegion);
            _currentViewport = viewport;
            Invalidated?.Invoke(
                this, new VisualSheetInvalidateArgs(_sheet.Range(viewport.VisibleRegion).Positions.ToHashSet()));
        }
        else
        {
            if (_currentViewport.VisibleRegion.Contains(viewport.VisibleRegion))
                return;

            var oldRegions = _currentViewport.VisibleRegion.Break(viewport.VisibleRegion);
            var newRegions = viewport.VisibleRegion.Break(_currentViewport.VisibleRegion);

            _currentViewport = viewport;
            // Clear where we don't need to store anymore.
            RemoveRegionsFromCache(oldRegions);
            // Store Visual Cells from the new regions that we just encountered.
            InvalidateRegions(newRegions);
            Invalidated?.Invoke(this, new VisualSheetInvalidateArgs(_sheet.Range(newRegions).Positions.ToHashSet()));
        }
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
        var range = _sheet.Range(regions);
        foreach (var cellPosition in range.Positions)
        {
            if (_visualCache.ContainsKey(cellPosition))
                _visualCache.Remove(cellPosition);
        }
    }

    private void InvalidateRegions(IEnumerable<IRegion> regions)
    {
        if (_currentViewport == null) // only happens when we are starting up
            InvalidateCells(_sheet.Range(regions).Positions);
        else
        {
            var regionsInViewport =
                regions.Select(x => x.GetIntersection(_currentViewport.VisibleRegion))
                       .Where(x => x != null);

            var cells = _sheet.Range(regionsInViewport!).Positions;
            InvalidateCells(cells);
        }
    }

    private void InvalidateCells(IEnumerable<(int row, int col)> cellPositions)
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
        if (!_visualCache.TryAdd((row, col), visualCell))
            _visualCache[(row, col)] = visualCell;
    }

    /// <summary>
    /// Returns the visual cell in the cache, at the position specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public VisualCell GetVisualCell(int row, int col)
    {
        if (_visualCache.TryGetValue((row, col), out var cell))
            return cell;
        return VisualCell.Empty(row, col, _sheet, ref _defaultFormat);
    }
}