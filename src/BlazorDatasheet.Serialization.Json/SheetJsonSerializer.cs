using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Serialization.Json.Converters;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json;

public class SheetJsonSerializer
{
    public void Serialize(Workbook workbook, Stream stream)
    {
        var workbookModel = WorkbookModel.Create(workbook);
        JsonSerializer.Serialize(stream, workbookModel, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new CellJsonConverter() }
        });
    }

    public string Serialize(Workbook workbook)
    {
        using var stream = new MemoryStream();
        Serialize(workbook, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}