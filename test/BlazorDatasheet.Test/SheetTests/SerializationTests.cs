using System;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SerializationTests
{
    [Test]
    public void SerializationTest()
    {
        Assert.DoesNotThrow(() =>
        {
            var sheet = new Sheet(10, 10);
            sheet.Cells["A1"]!.Value = "Hello, World!";
            sheet.Cells["A2"]!.Value = 42;
            sheet.Cells["A3"]!.Value = 3.14;
            sheet.Cells["A4"]!.Value = true;
            sheet.Cells["A5"]!.Value = new DateTime(2021, 1, 1);
            sheet.Cells["A7"]!.Formula = "=sum(A2:A3)";
            
            var s = new SheetJsonSerializer();
            var json = s.Serialize(sheet.Workbook);
            
            var sheetDeserialized = new SheetJsonDeserializer().Deserialize(json);
        });
    }
}