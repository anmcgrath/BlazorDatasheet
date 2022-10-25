using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using BlazorDatasheet.Data;
using BlazorDatasheet.Data.Events;
using BlazorDatasheet.Data.SpatialDataStructures;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatManager
{
    private readonly Sheet _sheet;

    private List<ConditionalFormatAbstractBase> _registered = new();
    private Dictionary<(int row, int id), CellConditionalFormatContainer?> _cache = new();

    private RTree<ConditonalFormatSpatialData> _cfTree = new();

    public ConditionalFormatManager(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.CellsChanged += HandleCellsChanged;
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a region
    /// </summary>
    /// <param name="region"></param>
    public void Apply(ConditionalFormatAbstractBase conditionalFormat, IRegion? region)
    {
        Apply(conditionalFormat, new BRange(_sheet, region));
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a range
    /// </summary>
    /// <param name="region"></param>
    public void Apply(ConditionalFormatAbstractBase conditionalFormat, BRange? range)
    {
        if (range == null)
            return;
        if (!_registered.Contains(conditionalFormat))
        {
            _registered.Add(conditionalFormat);
            conditionalFormat.Order = _registered.Count - 1;
        }

        conditionalFormat.Add(range, _sheet);
        foreach (var region in range.Regions)
        {
            _cfTree.Insert(new ConditonalFormatSpatialData(conditionalFormat, region));
        }

        conditionalFormat.Prepare(_sheet);
    }

    /// <summary>
    /// Applies conditional format to the whole sheet
    /// </summary>
    /// <param name="key"></param>
    public void Apply(ConditionalFormatAbstractBase format)
    {
        Apply(format, _sheet.Region);
    }

    private void HandleCellsChanged(object? sender, IEnumerable<ChangeEventArgs> args)
    {
        // Simply prepare all cells that the conditional format belongs to (if shared)
        var handled = new HashSet<int>();
        foreach (var changeEvent in args)
        {
            var cfs = GetFormatsAppliedToPosition(changeEvent.Row, changeEvent.Col);
            foreach (var format in cfs)
            {
                if (handled.Contains(format.Order))
                    continue;
                // prepare format (re-compute shared format cache etch.)
                if (format.IsShared)
                    format.Prepare(_sheet);
                handled.Add(format.Order);
            }
        }
    }

    private IEnumerable<IReadOnlyCell> GetCellsInFormat(ConditionalFormat format)
    {
        return format.Ranges.SelectMany(x => x.GetCells());
    }

    private IEnumerable<ConditionalFormatAbstractBase> GetFormatsAppliedToPosition(int row, int col)
    {
        return _cfTree.Search(new Envelope(col, row, col, row))
                      .Select(x => x.ConditionalFormat);
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a region.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="region"></param>
    public void Apply(ConditionalFormatAbstractBase format, int row, int col)
    {
        Apply(format, new Region(row, col));
    }

    public Format? GetFormat(int row, int col)
    {
        if (!_sheet.Region.Contains(row, col))
            return null;
        var cfs = GetFormatsAppliedToPosition(row, col);
        Format? initialFormat = null;
        foreach (var format in cfs)
        {
            var apply = format.Predicate?.Invoke((row, col), _sheet);
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

    internal class ConditonalFormatSpatialData : ISpatialData
    {
        internal ConditionalFormatAbstractBase _conditionalFormat;
        public ref readonly ConditionalFormatAbstractBase ConditionalFormat => ref _conditionalFormat;
        private readonly Envelope _envelope;
        public ref readonly Envelope Envelope => ref _envelope;

        internal ConditonalFormatSpatialData(ConditionalFormatAbstractBase cf, IRegion region)
        {
            _envelope = new Envelope(region.TopLeft.Col, region.TopLeft.Row, region.BottomRight.Col,
                                     region.BottomRight.Row);
            _conditionalFormat = cf;
        }
    }
}