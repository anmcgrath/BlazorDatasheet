using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetMetaDataCommand : BaseCommand, IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly string _name;
    private readonly object? _value;
    private object? _oldValue;

    protected override bool CanExecuteCore(Sheet sheet) => (sheet.Region.Contains(_row, _col));
    public override bool CanExecuteProtected(Sheet sheet) => true;

    public SetMetaDataCommand(int row, int col, string name, object? value)
    {
        _row = row;
        _col = col;
        _name = name;
        _value = value;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        _oldValue = sheet.Cells.GetMetaData(_row, _col, _name);
        sheet.Cells.SetMetaDataImpl(_row, _col, _name, _value);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.SetMetaDataImpl(_row, _col, _name, _oldValue);
        return true;
    }
}