using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public abstract class RegionCommand : BaseCommand, IUndoableCommand
{
    public IRegion Region { get; }

    protected RegionCommand(IRegion region)
    {
        Region = region;
    }

    public override bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.BatchUpdates();
        var executed = DoExecute(sheet);
        sheet.MarkDirty(Region);
        sheet.EndBatchUpdates();
        sheet.ScreenUpdating = true;
        return executed;
    }

    protected abstract bool DoExecute(Sheet sheet);
    protected abstract bool DoUndo(Sheet sheet);

    public bool Undo(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.BatchUpdates();
        var undone = DoUndo(sheet);
        sheet.EndBatchUpdates();
        sheet.MarkDirty(Region);
        sheet.ScreenUpdating = true;
        return undone;
    }
}