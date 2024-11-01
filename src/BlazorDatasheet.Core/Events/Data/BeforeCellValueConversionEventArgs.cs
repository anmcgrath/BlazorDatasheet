using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Events.Data;

public class BeforeCellValueConversionEventArgs
{
    /// <summary>
    /// The value that was entered/set by the user
    /// </summary>
    public object? OriginalValue { get; }

    /// <summary>
    /// The value that will be stored after converting <see cref="OriginalValue"/>. Set this to change the value being stored.
    /// </summary>
    public CellValue NewValue { get; set; }

    public int Row { get; }

    public int Column { get; }

    public string CellType { get; }

    public BeforeCellValueConversionEventArgs(object? originalValue, CellValue newValue, int row, int column, string cellType)
    {
        OriginalValue = originalValue;
        NewValue = newValue;
        Row = row;
        Column = column;
        CellType = cellType;
    }
}