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
            new CellFormatJsonConverter(),
            new CellJsonConverter(),
            new ConditionalFormatJsonConverter(Resolvers.ConditionalFormat),
            new ColorJsonConverter(),
            new DataValidationJsonConverter(Resolvers.DataValidation),
            new IFilterJsonConverter(Resolvers.Filter),
            new CellValueJsonConverter(),
            new VariableJsonConverter()
        };
    }

    private JsonSerializerOptions? _options;
    private JsonConverter[]? _optionsConverters;

    /// <summary>
    /// The options the last deserialization used, rebuilt only when <see cref="Converters"/> has
    /// changed since. A <see cref="JsonSerializerOptions"/> caches the reflected metadata for every
    /// type it has seen, so a fresh instance per call pays for that reflection every time - reusing
    /// one deserializer across loads is what makes the saving.
    /// </summary>
    private JsonSerializerOptions GetOptions()
    {
        if (_options != null && _optionsConverters != null && _optionsConverters.Length == Converters.Count)
        {
            var unchanged = true;
            for (var i = 0; i < _optionsConverters.Length; i++)
            {
                if (!ReferenceEquals(_optionsConverters[i], Converters[i]))
                {
                    unchanged = false;
                    break;
                }
            }

            if (unchanged)
                return _options;
        }

        var options = new JsonSerializerOptions();
        foreach (var converter in Converters)
            options.Converters.Add(converter);

        _optionsConverters = Converters.ToArray();
        _options = options;
        return options;
    }

    /// <param name="json">The serialized workbook.</param>
    /// <param name="formulaOptions">Options the loaded workbook's formula engine is created with.</param>
    /// <param name="calculate">
    /// When false, no formula is evaluated while loading. The caller must then run
    /// <c>workbook.GetFormulaEngine().CalculateSheet(true)</c> before reading any value that comes
    /// from a formula; doing so gives exactly the workbook that loading with true gives.
    /// </param>
    public Workbook Deserialize(string json, FormulaOptions? formulaOptions = null, bool calculate = true)
    {
        var workbookModel = JsonSerializer.Deserialize<WorkbookModel>(json, GetOptions());
        if (workbookModel is null)
            return new Workbook(formulaOptions);

        return WorkbookMapper.FromModel(workbookModel, formulaOptions, calculate);
    }
}
