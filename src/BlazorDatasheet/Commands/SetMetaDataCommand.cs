using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetMetaDataCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly string _name;
    private readonly object? _value;
    private object? _oldValue;

    public SetMetaDataCommand(int row, int col, string name, object? value)
    {
        _row = row;
        _col = col;
        _name = name;
        _value = value;
    }

    public bool Execute(Sheet sheet)
    {
        _oldValue = sheet.GetMetaData(_row, _col, _name);
        sheet.SetMetaDataImpl(_row, _col, _name, _value);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetMetaDataImpl(_row, _col, _name, _oldValue);
        return true;
    }
}