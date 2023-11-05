using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class AutoFillCommand : IUndoableCommand
{
    private IRegion _fromRegion;
    private IRegion _toRegion;

    private ClearCellsCommand? _clearCellsCommand = null;

    public AutoFillCommand(IRegion fromRegion, IRegion toRegion)
    {
        _fromRegion = fromRegion;
        _toRegion = toRegion;
    }

    public bool Execute(Sheet sheet)
    {
        if (_fromRegion.Contains(_toRegion))
            DoCut(sheet);

        return true;
    }

    private void DoCut(Sheet sheet)
    {
        Console.WriteLine("Cutting");
        _clearCellsCommand = new ClearCellsCommand(sheet.Range(_fromRegion.Break(_toRegion)));
        _clearCellsCommand.Execute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        if (_clearCellsCommand != null)
            _clearCellsCommand.Undo(sheet);

        return true;
    }
}