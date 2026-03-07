using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class FreezeRowColsCommand : BaseCommand, IUndoableCommand
{
    private FreezeState _oldFreezeState = null!;
    private readonly int _freezeTop;
    private readonly int _freezeBottom;
    private readonly int _freezeLeft;
    private readonly int _freezeRight;

    public FreezeRowColsCommand(int freezeTop, int freezeBottom, int freezeLeft, int freezeRight)
    {
        _freezeTop = freezeTop;
        _freezeBottom = freezeBottom;
        _freezeLeft = freezeLeft;
        _freezeRight = freezeRight;
    }

    public override bool Execute(Sheet sheet)
    {
        _oldFreezeState = sheet.FreezeState;
        sheet.FreezeRowColsImpl(_freezeTop, _freezeBottom, _freezeLeft, _freezeRight);
        return true;
    }

    public override bool CanExecute(Sheet sheet)
    {
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.FreezeRowColsImpl(_oldFreezeState.Top, _oldFreezeState.Bottom, _oldFreezeState.Left,
            _oldFreezeState.Right);
        return true;
    }
}