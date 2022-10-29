using BlazorDatasheet.Data;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats.DefaultConditionalFormats;

/// <summary>
/// Designed to be used with the Object Editor, affects the whole row or column
/// Of the object editor when any of the properties change.
/// </summary>
internal class DataExpressionConditionalFormat<T> : ConditionalFormatAbstractBase
{
    private readonly Func<T, Format?> _conditionalFormatFunc;
    public T? Data { get; set; }

    internal DataExpressionConditionalFormat(T? data, Predicate<T> dataPredicate,
        Func<T, Format?> conditionalFormatFunc)
    {
        Data = data;
        _conditionalFormatFunc = conditionalFormatFunc;
        this.Predicate = (posn, sheet) => { return Data != null && dataPredicate.Invoke(Data); };
        this.IsShared = true;
    }

    public override Format? CalculateFormat(int row, int col, Sheet sheet)
    {
        return _conditionalFormatFunc.Invoke(Data);
    }
}