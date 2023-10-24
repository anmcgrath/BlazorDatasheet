using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Formats.DefaultConditionalFormats;

/// <summary>
/// Designed to only be used with the Object Editor, affects the whole row or column
/// Of the object editor when any of the properties change.
/// </summary>
internal class DataExpressionConditionalFormat<T> : ConditionalFormatAbstractBase
{
    private readonly Func<T, CellFormat?> _conditionalFormatFunc;
    public T? Data { get; set; }

    internal DataExpressionConditionalFormat(T? data, Predicate<T> dataPredicate,
        Func<T, CellFormat?> conditionalFormatFunc)
    {
        Data = data;
        _conditionalFormatFunc = conditionalFormatFunc;
        this.Predicate = (posn, sheet) => { return Data != null && dataPredicate.Invoke(Data); };
        this.IsShared = true;
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        return _conditionalFormatFunc.Invoke(Data);
    }
}