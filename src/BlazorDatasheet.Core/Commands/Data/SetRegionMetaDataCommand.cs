using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Sets one metadata key to the same value throughout a region.
/// </summary>
public class SetRegionMetaDataCommand : BaseCommand, IUndoableCommand
{
    private readonly IRegion _region;
    private readonly string _name;
    private readonly object? _value;
    private List<(int Row, int Col, object? Value)> _oldValues = new();

    public SetRegionMetaDataCommand(IRegion region, string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(name);
        _region = region.Clone();
        _name = name;
        _value = value;
    }

    public override bool CanExecute(Sheet sheet) => sheet.Region.Contains(_region);

    public override bool Execute(Sheet sheet)
    {
        // Capture the whole region before emitting any metadata events while applying the write.
        _oldValues = new SheetRange(sheet, _region).Positions
            .Select(position => (position.row, position.col,
                sheet.Cells.GetMetaData(position.row, position.col, _name)))
            .ToList();

        sheet.BatchUpdates();
        try
        {
            foreach (var cell in _oldValues)
                sheet.Cells.SetMetaDataImpl(cell.Row, cell.Col, _name, _value);
        }
        finally
        {
            sheet.EndBatchUpdates();
        }

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.BatchUpdates();
        try
        {
            foreach (var cell in _oldValues)
                sheet.Cells.SetMetaDataImpl(cell.Row, cell.Col, _name, cell.Value);
        }
        finally
        {
            sheet.EndBatchUpdates();
        }

        return true;
    }
}
