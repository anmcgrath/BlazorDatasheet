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

    public IRegion Region { get; private set; }

    /// <summary>
    /// Return the positions present in the range. May be non-unique & include empty position
    /// depending on the regions present.
    /// </summary>
    public IEnumerable<CellPosition> Positions => _rangePositionEnumerator;

    private readonly RangePositionEnumerator _rangePositionEnumerator;

    internal SheetRange(Sheet sheet, int row, int col) : this(sheet, new Region(row, col))
    {
    }

    internal SheetRange(Sheet sheet, IRegion region)
    {
        Sheet = sheet;
        _rangePositionEnumerator = new(this);
        Region = region;
    }

    /// <summary>
    /// Sets the value of all cells in the range.
    /// </summary>
    public object? Value
    {
        set => DoSetValues(value);
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
        return Sheet.Cells.GetNonEmptyCellPositions(Region);
    }


    internal IEnumerable<IReadOnlyCell> GetCells()
    {
        return Sheet.Cells.GetCellsInRegion(Region);
    }

    /// <summary>
    /// Sets all the cells in the range to the value specified.
    /// </summary>
    /// <param name="value"></param>
    private void DoSetValues(object? value)
    {
        Sheet.Cells.SetValues(Positions.Select(x => (x.row, x.col, value)).ToList());
    }

    public void Clear()
    {
        Sheet.Cells.ClearCells(this.Region);
    }

    internal SheetRange Clone()
    {
        return new SheetRange(this.Sheet, Region.Clone());
    }

    /// <summary>
    /// Sets the sheet's selection to this range.
    /// </summary>
    public void Select()
    {
        Sheet.Selection.ClearSelections();
        Sheet.Selection.Set(this.Region);
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
        set => Sheet.Cells.SetType(Region, value);
    }

    public CellFormat? Format
    {
        set => Sheet.SetFormat(Region, value);
    }

    public void AddValidator(IDataValidator validator)
    {
        Sheet.BatchUpdates();
        Sheet.Validators.AddImpl(validator, Region);
        Sheet.EndBatchUpdates();
    }

    public void Merge()
    {
        Sheet.Cells.Merge(this.Region);
    }
}