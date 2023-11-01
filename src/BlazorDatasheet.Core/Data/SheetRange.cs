using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Metadata;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Specifies a range, which is a collection of regions in a sheet.
/// </summary>
public class SheetRange
{
    internal readonly Sheet Sheet;
    protected List<IRegion> _regions = new();

    public IReadOnlyList<IRegion> Regions
    {
        get => _regions;
        set => _regions = value.ToList();
    }

    /// <summary>
    /// Return the positions present in the range. May be non-unique & include empty position
    /// depending on the regions present.
    /// </summary>
    public IEnumerable<CellPosition> Positions => _rangePositionEnumerator;

    private readonly RangePositionEnumerator _rangePositionEnumerator;

    internal SheetRange(Sheet sheet, List<IRegion> regions)
    {
        Sheet = sheet;
        Regions = regions;
        _rangePositionEnumerator = new RangePositionEnumerator(this);
    }

    internal SheetRange(Sheet sheet, int row, int col) : this(sheet, new Region(row, col))
    {
    }

    /// <summary>
    /// Create a range with the expression e.g A1, A:B, A1:A5, Allows multiple regions by seperating with a ',' etc.
    /// </summary>
    /// <param name="rangeExpression"></param>
    internal SheetRange(Sheet sheet, string rangeExpression)
    {
        Sheet = sheet;
        _rangePositionEnumerator = new RangePositionEnumerator(this);

        if (string.IsNullOrEmpty(rangeExpression))
            return;

        foreach (var split in rangeExpression.Split(","))
        {
            var region = Region.FromString(split);
            if (region != null)
                _regions.Add(region);
        }
    }

    internal SheetRange(Sheet sheet, IRegion region) :
        this(sheet, new List<IRegion>() { region })
    {
    }

    /// <summary>
    /// Sets the value of all cells in the range.
    /// </summary>
    public object? Value
    {
        set => doSetValues(value);
    }

    /// <summary>
    /// Return all non-empty cells (in terms of Value) in this range.
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<IReadOnlyCell> GetNonEmptyCells()
    {
        return this.GetNonEmptyPositions().Select(x => Sheet.Cells.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Return all positions of non-empty cells (in terms of Value) in the sheet.
    /// </summary>
    /// <returns>A collection of (row, column) positions of all non-empty cells.</returns>
    internal IEnumerable<CellPosition> GetNonEmptyPositions()
    {
        return Regions.SelectMany(Sheet.Cells.GetNonEmptyCellPositions);
    }

    /// <summary>
    /// Sets all the cells in the range to the value specified.
    /// </summary>
    /// <param name="value"></param>
    private void doSetValues(object? value)
    {
        Sheet.Cells.SetValues(Positions.Select(x => (x.row, x.col, value)).ToList());
    }

    public void Clear()
    {
        Sheet.Cells.ClearCells(this);
    }

    internal void AddRegion(IRegion region)
    {
        _regions.Add(region);
    }

    internal void RemoveRegion(IRegion region)
    {
        _regions.Remove(region);
    }

    internal SheetRange Clone()
    {
        return new SheetRange(this.Sheet, _regions.Select(x => x.Clone()).ToList());
    }

    /// <summary>
    /// Sets the sheet's selection to this range.
    /// </summary>
    public void Select()
    {
        Sheet.Selection.ClearSelections();
        Sheet.Selection.Set(this);
    }

    public void SetMetaData(string name, object? value)
    {
        Sheet.BatchUpdates();
        foreach (var cellPosition in this.Positions)
        {
            Sheet.Cells.SetMetaDataImpl(cellPosition.row, cellPosition.col, name, value);
        }

        Sheet.EndBatchUpdates();
    }

    /// <summary>
    /// Set the cell type e.g "text", "boolean", "select" on the range.
    /// </summary>
    public string Type
    {
        set => Sheet.Cells.SetType(_regions, value);
    }

    public CellFormat? Format
    {
        set => Sheet.SetFormat(value, this);
    }

    public void AddValidator(IDataValidator validator)
    {
        Sheet.BatchUpdates();
        foreach (var region in _regions)
            Sheet.Validators.AddImpl(validator, region);
        Sheet.EndBatchUpdates();
    }
}