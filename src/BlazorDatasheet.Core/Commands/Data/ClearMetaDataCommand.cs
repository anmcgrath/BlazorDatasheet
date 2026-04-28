using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Metadata;

namespace BlazorDatasheet.Core.Commands.Data;

public class ClearMetaDataCommand : BaseCommand, IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private CellMetadata? _oldMetaData;

    public ClearMetaDataCommand(int row, int col)
    {
        _row = row;
        _col = col;
    }

    public override bool CanExecute(Sheet sheet) => sheet.Region.Contains(_row, _col);

    public override bool Execute(Sheet sheet)
    {
        _oldMetaData = sheet.Cells.GetCellMetaData(_row, _col)?.Clone();
        sheet.Cells.ClearMetaDataImpl(_row, _col);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.SetMetaDataImpl(_row, _col, _oldMetaData?.Clone());
        return true;
    }
}
