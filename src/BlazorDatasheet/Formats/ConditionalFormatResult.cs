using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatResult
{
    public int Index { get; }
    public Format? Format { get; }
    public bool IsTrue { get; }
    public bool StopIfTrue { get; }

    /// <summary>
    /// Stores the result of a conditional format run
    /// </summary>
    /// <param name="index">The unique conditional format index</param>
    /// <param name="format">The format result</param>
    /// <param name="isTrue">Whether the predicate returned true when running the format</param>
    /// <param name="stopIfTrue"></param>
    public ConditionalFormatResult(int index, Format? format, bool isTrue, bool stopIfTrue)
    {
        Index = index;
        Format = format;
        IsTrue = isTrue;
        StopIfTrue = stopIfTrue;
    }
}