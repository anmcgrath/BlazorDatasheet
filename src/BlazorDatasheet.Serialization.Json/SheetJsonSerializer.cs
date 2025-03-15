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
    private readonly Func<string, Type?> _dataValidationTypeResolver;
    private readonly Func<string, Type?> _conditionalFormatTypeResolver;
    private readonly Func<string, Type?> _filterTypeResolver;

    public SheetJsonSerializer(
        Func<string, Type?>? conditionalFormatTypeResolver = null,
        Func<string, Type?>? dataValidationTypeResolver = null,
        Func<string, Type?>? filterTypeResolver = null)
    {
        _conditionalFormatTypeResolver = conditionalFormatTypeResolver ?? (_ => null);
        _dataValidationTypeResolver = dataValidationTypeResolver ?? (_ => null);
        _filterTypeResolver = filterTypeResolver ?? (_ => null);
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
                new VariableJsonConverter(),
                new DataValidationJsonConverter(_dataValidationTypeResolver),
                new IFilterJsonConverter(_filterTypeResolver)
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