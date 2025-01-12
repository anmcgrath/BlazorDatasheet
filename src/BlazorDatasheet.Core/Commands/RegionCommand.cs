using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public abstract class RegionCommand : BaseCommand, IUndoableCommand
{
    private readonly List<IUndoableCommand> _children = new();
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
        for (int i = _children.Count - 1; i >= 0; i--)
            _children[i].Undo(sheet);
        sheet.ScreenUpdating = false;
        sheet.BatchUpdates();
        var undone = DoUndo(sheet);
        sheet.EndBatchUpdates();
        sheet.MarkDirty(Region);
        sheet.ScreenUpdating = true;

        return undone;
    }

    public void Attach(IUndoableCommand command)
    {
        _children.Add(command);
    }
}