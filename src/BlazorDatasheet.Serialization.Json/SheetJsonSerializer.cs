using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Contracts;
using BlazorDatasheet.Serialization.Json.Converters;
using BlazorDatasheet.Serialization.Json.Mappers;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json;

public class SheetJsonSerializer
{
    public void Serialize(Workbook workbook, Stream stream)
    {
        var workbookModel = WorkbookMapper.FromWorkbook(workbook);
        JsonSerializer.Serialize(stream, workbookModel, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new CellJsonConverter(),
                new ConditionalFormatJsonConverter(),
                new ColorJsonConverter()
            },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            {
                Modifiers = { DatasheetContracts.IgnoreEmptyArray }
            }
        });
    }

    public string Serialize(Workbook workbook)
    {
        using var stream = new MemoryStream();
        Serialize(workbook, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}