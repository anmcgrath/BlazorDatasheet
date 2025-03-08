using System.Text.Json;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Converters;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json;

public class SheetJsonDeserializer
{
    public Workbook Deserialize(string json)
    {
        var options = new JsonSerializerOptions()
        {
            Converters = { new CellJsonConverter() }
        };

        var workbookModel = JsonSerializer.Deserialize<WorkbookModel>(json, options);
        if (workbookModel is null)
            return new Workbook();

        return workbookModel.FromModel();
    }
}