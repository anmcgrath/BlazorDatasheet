using System.Text.Json;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Converters;
using BlazorDatasheet.Serialization.Json.Mappers;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json;

public class SheetJsonDeserializer
{
    private readonly Func<string, Type?> _conditionalFormatTypeResolver;

    public SheetJsonDeserializer(Func<string, Type?>? conditionalFormatTypeResolver = null)
    {
        if (conditionalFormatTypeResolver == null)
            _conditionalFormatTypeResolver = _ => null;
        else
            _conditionalFormatTypeResolver = conditionalFormatTypeResolver;
    }

    public Workbook Deserialize(string json)
    {
        var options = new JsonSerializerOptions()
        {
            Converters =
            {
                new CellJsonConverter(),
                new ConditionalFormatJsonConverter(_conditionalFormatTypeResolver),
                new ColorJsonConverter()
            },
        };

        var workbookModel = JsonSerializer.Deserialize<WorkbookModel>(json, options);
        if (workbookModel is null)
            return new Workbook();

        return WorkbookMapper.FromModel(workbookModel);
    }
}