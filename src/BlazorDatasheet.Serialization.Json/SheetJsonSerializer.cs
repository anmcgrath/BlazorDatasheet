using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Contracts;
using BlazorDatasheet.Serialization.Json.Converters;
using BlazorDatasheet.Serialization.Json.Mappers;

namespace BlazorDatasheet.Serialization.Json;

public class SheetJsonSerializer
{
    private readonly Func<string, Type?> _conditionalFormatTypeResolver;

    public SheetJsonSerializer(Func<string, Type?>? conditionalFormatTypeResolver = null)
    {
        if (conditionalFormatTypeResolver == null)
            _conditionalFormatTypeResolver = _ => null;
        else
            _conditionalFormatTypeResolver = conditionalFormatTypeResolver;
    }

    public void Serialize(Workbook workbook, Stream stream, bool writeIndented = false)
    {
        var workbookModel = WorkbookMapper.FromWorkbook(workbook);
        JsonSerializer.Serialize(stream, workbookModel, new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new CellJsonConverter(),
                new ConditionalFormatJsonConverter(_conditionalFormatTypeResolver),
                new ColorJsonConverter(),
                new VariableJsonConverter()
            },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            {
                Modifiers = { DatasheetContracts.IgnoreEmptyArray }
            }
        });
    }

    public string Serialize(Workbook workbook, bool writeIndented = false)
    {
        using var stream = new MemoryStream();
        Serialize(workbook, stream, writeIndented);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}