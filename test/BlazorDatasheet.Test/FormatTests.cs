using BlazorDatasheet.Formats;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class FormatTests
{
    private ConditionalFormatManager cfm;

    [SetUp]
    public void SetupManager()
    {
        cfm = new ConditionalFormatManager();
    }

    [SetUp]
    public void Apply_Cf_To_Range_Then_Get_Cf_Works()
    {
        
    }
}