using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Formatting;

public class ClearFormatCommand : BaseCommand, IUndoableCommand
{
    public IRegion Region { get; }
    private CellStoreRestoreData? _restoreData;

    public ClearFormatCommand(IRegion region)
    {
        Region = region.Clone();
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        sheet.BatchUpdates();
        var region = sheet.Region.GetIntersection(Region);
        if (region != null)
        {
            _restoreData = sheet.Cells.CutFormatImpl(region);
            sheet.MarkDirty(region);
        }

        sheet.EndBatchUpdates();
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_restoreData == null)
            return true;

        sheet.BatchUpdates();
        sheet.Cells.Restore(_restoreData);
        sheet.MarkDirty(Region);
        sheet.EndBatchUpdates();
        return true;
    }
}
