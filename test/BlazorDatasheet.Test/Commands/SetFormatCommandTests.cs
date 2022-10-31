using BlazorDatasheet.Data;
using BlazorDatasheet.Render;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetFormatCommandTests
{
    [Test]
    public void Set_Format_And_Undo_Sets_Correctly()
    {
        var f1 = new Format() { BackgroundColor = "f1" };
        var f2 = new Format() { BackgroundColor = "f2" };
        var sheet = new Sheet(3, 3);
        
        //sheet.SetFormat(f1, sheet.Range());
    }
}