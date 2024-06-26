using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Formats;

public class ConditionalFormatManager
{
    private readonly Sheet _sheet;

    private readonly List<ConditionalFormatAbstractBase> _registered = new();
    private readonly ConsolidatedDataStore<ConditionalFormatAbstractBase> _appliedFormats = new();

    public ConditionalFormatManager(Sheet sheet,
        CellStore cellStore)
    {
        _sheet = sheet;
        cellStore.CellsChanged += HandleCellsChanged;
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a region
    /// </summary>
    /// <param name="region"></param>
    /// <param name="conditionalFormat"></param>
    public void Apply(IRegion? region, ConditionalFormatAbstractBase conditionalFormat)
    {
        Apply(new SheetRange(_sheet, region), conditionalFormat);
    }

    /// <summary>
    /// Applies the conditional format to the region
    /// </summary>
    /// <param name="range"></param>
    /// <param name="conditionalFormat"></param>
    public void Apply(SheetRange? range, ConditionalFormatAbstractBase conditionalFormat)
    {
        if (range == null)
            return;

        if (!_registered.Contains(conditionalFormat))
        {
            _registered.Add(conditionalFormat);
            conditionalFormat.Order = _registered.Count - 1;
        }

        _appliedFormats.Add(range.Region, conditionalFormat);
        Prepare(new List<ConditionalFormatAbstractBase>() { conditionalFormat });
    }

    private List<SheetRange> GetRangesAppliedToFormat(ConditionalFormatAbstractBase format)
    {
        var regions = _appliedFormats.GetRegions(format);
        return regions.Select(x => new SheetRange(_sheet, x)).ToList();
    }


    private void HandleCellsChanged(object? sender, CellDataChangedEventArgs args)
    {
        // Simply prepare all cells that the conditional format belongs to (if shared)
        var cellCfs =
            args.Positions
                .SelectMany(x => GetFormatsAppliedToPosition(x.row, x.col));

        var rangeCfs = args.Regions
            .SelectMany(x => GetFormatsAppliedToRegion(x));

        var cfs = cellCfs.Concat(rangeCfs)
            .Distinct()
            .ToList();

        Prepare(cfs);
    }

    private void Prepare(List<ConditionalFormatAbstractBase> formats)
    {
        foreach (var format in formats)
        {
            // prepare format (re-compute shared format cache etch.)
            if (format.IsShared)
            {
                format.Prepare(GetRangesAppliedToFormat(format));
                _sheet.MarkDirty(GetRangesAppliedToFormat(format).Select(x => x.Region));
            }
        }
    }

    private IEnumerable<ConditionalFormatAbstractBase> GetFormatsAppliedToPosition(int row, int col)
    {
        return _appliedFormats.GetData(row, col);
    }

    private IEnumerable<ConditionalFormatAbstractBase> GetFormatsAppliedToRegion(IRegion region)
    {
        return _appliedFormats.GetData(region);
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a region.
    /// <param name="format"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// </summary>
    public void Apply(int row, int col, ConditionalFormatAbstractBase format)
    {
        Apply(new Region(row, col), format);
    }

    /// <summary>
    /// Returns the format that results from applying all conditional formats to this cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public CellFormat? GetFormatResult(int row, int col)
    {
        if (!_sheet.Region.Contains(row, col))
            return null;

        var cfs = GetFormatsAppliedToPosition(row, col);
        CellFormat? initialFormat = null;
        foreach (var format in cfs)
        {
            var apply = format.Predicate?.Invoke(new CellPosition(row, col), _sheet);
            if (apply == false)
                continue;
            var calced = format.CalculateFormat(row, col, _sheet);
            if (initialFormat == null)
                initialFormat = calced;
            else
                initialFormat.Merge(calced);
            if (apply == true && format.StopIfTrue)
                break;
        }

        return initialFormat;
    }


    internal RegionRestoreData<ConditionalFormatAbstractBase> InsertRowColAt(int row, int nRows, Axis axis)
    {
        return _appliedFormats.InsertRowColAt(row, nRows, axis);
    }

    internal RegionRestoreData<ConditionalFormatAbstractBase> RemoveRowColAt(int index, int count, Axis axis)
    {
        IRegion dataRegion =
            axis == Axis.Row ? new RowRegion(index, int.MaxValue) : new ColumnRegion(index, int.MaxValue);

        var cfsAffected = _appliedFormats
            .GetData(dataRegion)
            .ToList();

        var restoreData = _appliedFormats.RemoveRowColAt(index, count, axis);
        Prepare(cfsAffected);

        return restoreData;
    }

    internal void Restore(RegionRestoreData<ConditionalFormatAbstractBase> data)
    {
        _appliedFormats.Restore(data);

        var cfsAffected = data.RegionsAdded
            .Select(x => x.Data)
            .Concat(data.RegionsRemoved.Select(x => x.Data));

        foreach (var shift in data.Shifts)
        {
            if (shift.Axis == Axis.Col)
                cfsAffected = cfsAffected.Concat(_appliedFormats.GetData(new ColumnRegion(shift.Index, int.MaxValue)));
            else
                cfsAffected = cfsAffected.Concat(_appliedFormats.GetData(new RowRegion(shift.Index, int.MaxValue)));
        }

        Prepare(cfsAffected.Distinct().ToList());
    }
}