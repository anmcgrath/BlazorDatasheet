using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatPreparedEventArgs
{
    public ConditionalFormatPreparedEventArgs(ConditionalFormatAbstractBase conditionalFormat)
    {
        ConditionalFormat = conditionalFormat;
    }

    public readonly ConditionalFormatAbstractBase ConditionalFormat;
}