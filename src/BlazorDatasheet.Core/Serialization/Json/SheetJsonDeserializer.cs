using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json.Converters;
using BlazorDatasheet.Core.Serialization.Json.Mappers;
using BlazorDatasheet.Core.Serialization.Models;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Serialization.Json;

public class SheetJsonDeserializer
{
    public SheetSerializationTypeResolverCollection Resolvers { get; } = new();
    public IList<JsonConverter> Converters { get; }

    public SheetJsonDeserializer()
    {
        Converters = new List<JsonConverter>()
        {
            new CellJsonConverter(),
            new ConditionalFormatJsonConverter(Resolvers.ConditionalFormat),
            new ColorJsonConverter(),
            new DataValidationJsonConverter(Resolvers.DataValidation),
            new IFilterJsonConverter(Resolvers.Filter),
            new CellValueJsonConverter(),
            new VariableJsonConverter()
        };
    }

    public Workbook Deserialize(string json, FormulaOptions? formulaOptions = null)
    {
        var options = new JsonSerializerOptions();
        foreach (var converter in Converters)
            options.Converters.Add(converter);

        var workbookModel = JsonSerializer.Deserialize<WorkbookModel>(json, options);
        if (workbookModel is null)
            return new Workbook(formulaOptions);

        return WorkbookMapper.FromModel(workbookModel, formulaOptions);
    }
}
