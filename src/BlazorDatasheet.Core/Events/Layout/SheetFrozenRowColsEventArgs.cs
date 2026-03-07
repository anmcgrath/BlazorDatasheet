using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Layout;

public class SheetFrozenRowColsEventArgs : EventArgs
{
    public FreezeState OldFreezeState { get; }
    public FreezeState NewFreezeState { get; }

    public SheetFrozenRowColsEventArgs(FreezeState oldFreezeState, FreezeState newFreezeState)
    {
        OldFreezeState = oldFreezeState;
        NewFreezeState = newFreezeState;
    }
}