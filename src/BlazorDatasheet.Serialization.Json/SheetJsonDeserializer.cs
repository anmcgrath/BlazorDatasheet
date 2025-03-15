using System.Text.Json;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Converters;
using BlazorDatasheet.Serialization.Json.Mappers;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json;

public class SheetJsonDeserializer
{
    private readonly Func<string, Type?> _dataValidationTypeResolver;
    private readonly Func<string, Type?> _conditionalFormatTypeResolver;
    private readonly Func<string, Type?> _filterTypeResolver;

    public SheetJsonDeserializer(
        Func<string, Type?>? conditionalFormatTypeResolver = null,
        Func<string, Type?>? dataValidationTypeResolver = null,
        Func<string, Type?>? filterTypeResolver = null)
    {
        _conditionalFormatTypeResolver = conditionalFormatTypeResolver ?? (_ => null);
        _dataValidationTypeResolver = dataValidationTypeResolver ?? (_ => null);
        _filterTypeResolver = filterTypeResolver ?? (_ => null);
    }

    public Workbook Deserialize(string json)
    {
        var options = new JsonSerializerOptions()
        {
            Converters =
            {
                new CellJsonConverter(),
                new ConditionalFormatJsonConverter(_conditionalFormatTypeResolver),
                new ColorJsonConverter(),
                new VariableJsonConverter(),
                new DataValidationJsonConverter(_dataValidationTypeResolver),
                new IFilterJsonConverter(_filterTypeResolver)
            },
        };

        var workbookModel = JsonSerializer.Deserialize<WorkbookModel>(json, options);
        if (workbookModel is null)
            return new Workbook();

        return WorkbookMapper.FromModel(workbookModel);
    }
}