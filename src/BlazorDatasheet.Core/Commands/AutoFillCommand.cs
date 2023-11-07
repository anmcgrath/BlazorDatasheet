using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class AutoFillCommand : IUndoableCommand
{
    private IRegion _fromRegion;
    private IRegion _toRegion;
    private List<CopyRangeCommand> _copyCommands = new();

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
        else
        {
            var newRegion = _toRegion.Break(_fromRegion).First(); // will always be only one region
            if (_fromRegion.Width == newRegion.Width) // vertical fill
            {
                var cmd = new CopyRangeCommand(sheet.Range(_fromRegion),
                    new[] { sheet.Range(newRegion) });
                cmd.Execute(sheet);
                _copyCommands.Add(cmd);
            }
        }

        sheet.Selection.Set(_toRegion);
        return true;
    }

    private void DoCut(Sheet sheet)
    {
        _clearCellsCommand = new ClearCellsCommand(_fromRegion.Break(_toRegion));
        _clearCellsCommand.Execute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        if (_clearCellsCommand != null)
            _clearCellsCommand.Undo(sheet);

        foreach (var cmd in _copyCommands)
            cmd.Undo(sheet);

        return true;
    }
}