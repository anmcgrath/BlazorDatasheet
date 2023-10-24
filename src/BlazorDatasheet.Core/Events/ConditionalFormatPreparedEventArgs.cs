using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Events;

public class ConditionalFormatPreparedEventArgs
{
    public ConditionalFormatPreparedEventArgs(ConditionalFormatAbstractBase conditionalFormat)
    {
        ConditionalFormat = conditionalFormat;
    }

    public readonly ConditionalFormatAbstractBase ConditionalFormat;
}