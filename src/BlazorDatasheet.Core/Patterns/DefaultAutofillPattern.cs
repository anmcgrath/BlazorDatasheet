using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Patterns;

public class DefaultAutofillPattern : IAutoFillPattern
{
    private readonly Sheet _sheet;
    public ICollection<int> Offsets { get; }

    public DefaultAutofillPattern(Sheet sheet, List<int> offsets)
    {
        Offsets = offsets;
        _sheet = sheet;
    }

    public ICommand GetCommand(int offset, int repeatNo, IReadOnlyCell cellData, CellPosition newDataPosition)
    {
        var options = new CopyOptions();
        if (cellData.HasFormula())
            options.CopyValues = false;

        return new CopyRangeCommand(_sheet.Range(cellData.Row, cellData.Col),
            _sheet.Range(newDataPosition.row, newDataPosition.col), options);
    }
}