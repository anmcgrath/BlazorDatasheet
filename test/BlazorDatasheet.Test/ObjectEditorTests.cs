using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Formats;
using BlazorDatasheet.ObjectEditor;
using BlazorDatasheet.Render;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class ObjectEditorTests
{
    private List<TesterObject> _items;

    [SetUp]
    public void Setup()
    {
        _items = new List<TesterObject>()
        {
            new TesterObject(1, false, "obj1"),
            new TesterObject(2, true, "obj2")
        };
    }

    [Test]
    public void Auto_Generate_Properties_CapturesAll_Props()
    {
        var builder = new ObjectEditorBuilder<TesterObject>(_items);
        builder.AutogenerateProperties(true);
        var editor = builder.Build();
        var sheet = editor.Sheet;

        var propNames = typeof(TesterObject).GetProperties().Select(x => x.Name);

        Assert.AreEqual(_items.Count, sheet.NumRows);
        Assert.AreEqual(propNames.Count(), sheet.ColumnHeadings.Count);
    }

    [Test]
    public void Apply_Conditional_Format_On_Obj_Editor_Span_Properties_Across_Cols_Is_Applied_Correctly()
    {
        var builder = new ObjectEditorBuilder<TesterObject>(_items, GridDirection.PropertiesAcrossColumns);
        // Create a conditional format that sets bg color to green when true
        var cf = new ConditionalFormat(
            (posn, sheet) => sheet.GetCell(posn.row, posn.col).GetValue<bool?>() == true,
            c => new CellFormat() { BackgroundColor = "green" });

        builder.AutogenerateProperties(false);
        // Define first column as PropString
        builder.WithProperty(x => x.PropString, pd => { });
        //Define second column as PropBool
        builder.WithProperty(x => x.PropBool, pd => { pd.ApplyConditionalFormat(cf); });

        var sheet = builder.Build().Sheet;
        // Format is not applied to the first column which is not PropBool
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 0));

        // Should be null the first time because the propBool = false
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 1));
        sheet.TrySetCellValue(0, 1, false);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 1));
    }

    [Test]
    public void Apply_Conditional_Format_On_Span_Properties_Across_Rows_Is_Applied_Correctly()
    {
        var builder = new ObjectEditorBuilder<TesterObject>(_items, GridDirection.PropertiesAcrossRows);
        // Create a conditional format that sets bg color to green when true
        var cf = new ConditionalFormat(
            (posn, sheet) => sheet.GetCell(posn.row, posn.col).GetValue<bool>() == true,
            c => new CellFormat() { BackgroundColor = "green" });

        builder.AutogenerateProperties(false);
        // Define first row as PropString
        builder.WithProperty(x => x.PropString, pd => { });
        //Define second row as PropBool
        builder.WithProperty(x => x.PropBool, pd => { pd.ApplyConditionalFormat(cf); });

        var sheet = builder.Build().Sheet;
        // Format is not applied to the first row which is not PropBool
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 0));

        // Should be null the first time because the propBool = false
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(1, 0));
        sheet.TrySetCellValue(0, 1, false);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(1, 0));
    }
}

internal class TesterObject
{
    public TesterObject(int propInt, bool propBool, string propString)
    {
        PropInt = propInt;
        PropBool = propBool;
        PropString = propString;
    }

    public int PropInt { get; set; }
    public bool PropBool { get; set; }
    public string PropString { get; set; }
}