using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.ObjectEditor;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.Render;
using FluentAssertions;
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
    public void TestObjectBuilder()
    {
        var builder = new ObjectEditorBuilder<TesterObject>(new EnumerableQuery<TesterObject>(_items))
            .WithProperty(x => x.PropInt,
                p => p.WithType("number")
                    .WithDataValidator(new NumberValidator(false)))
            .WithProperty(x => x.PropString)
            .WithProperty(x => x.PropBool);

        var editor = builder.Build();
        editor.Sheet.Cells[0, 0].Value.Should().Be(1);
        editor.Sheet.Cells[0, 1].Value.Should().Be("obj1");
        editor.Sheet.Cells[1, 0].Value.Should().Be(2);
        editor.Sheet.Cells[1, 1].Value.Should().Be("obj2");
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