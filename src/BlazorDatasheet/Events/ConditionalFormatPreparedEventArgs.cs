using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Events;

public class ConditionalFormatPreparedEventArgs
{
    public ConditionalFormatPreparedEventArgs(ConditionalFormatAbstractBase conditionalFormat)
    {
        ConditionalFormat = conditionalFormat;
    }

    public readonly ConditionalFormatAbstractBase ConditionalFormat;
}