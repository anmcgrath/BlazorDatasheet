using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json.Contracts;
using BlazorDatasheet.Core.Serialization.Json.Converters;
using BlazorDatasheet.Core.Serialization.Json.Mappers;

namespace BlazorDatasheet.Core.Serialization.Json;

public class SheetJsonSerializer
{
    private readonly List<string> _warnings = new();

    public SheetSerializationTypeResolverCollection Resolvers { get; } = new();

    /// <summary>
    /// Non-fatal issues encountered during the most recent call to <see cref="Serialize(Workbook, Stream, bool)"/>.
    /// Cleared at the start of each serialization.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    public void Serialize(Workbook workbook, Stream stream, bool writeIndented = false)
    {
        _warnings.Clear();
        var workbookModel = WorkbookMapper.FromWorkbook(workbook, _warnings.Add);
        JsonSerializer.Serialize(stream, workbookModel, new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new CellFormatJsonConverter(),
                new CellJsonConverter(),
                new ConditionalFormatJsonConverter(Resolvers.ConditionalFormat, _warnings.Add),
                new ColorJsonConverter(),
                new DataValidationJsonConverter(Resolvers.DataValidation),
                new IFilterJsonConverter(Resolvers.Filter),
                new VariableJsonConverter(),
                new CellValueJsonConverter()
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
