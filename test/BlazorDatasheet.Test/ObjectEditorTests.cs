using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.ObjectEditor;
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

        Assert.AreEqual(_items.Count, sheet.Rows.Count);
        Assert.AreEqual(propNames.Count(), sheet.ColumnHeadings.Count);
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