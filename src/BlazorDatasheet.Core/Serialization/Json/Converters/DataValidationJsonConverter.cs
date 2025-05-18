using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Core.Serialization.Json.Constants;
using BlazorDatasheet.Core.Serialization.Models;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class DataValidationJsonConverter : JsonConverter<DataRegionModel<IDataValidator>>
{
    private readonly Dictionary<string, Type> _resolver;

    public DataValidationJsonConverter(Dictionary<string, Type> resolver)
    {
        _resolver = resolver;
    }

    public override DataRegionModel<IDataValidator>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var validatorTypeName = string.Empty;
        JsonElement? parsedOptions = null;
        string regionString = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case JsonConstants.RangeReference:
                    regionString = reader.GetString();
                    break;
                case JsonConstants.ClassType:
                    validatorTypeName = reader.GetString();
                    break;
                case JsonConstants.Options:
                    parsedOptions = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(validatorTypeName))
            return null;

        if (parsedOptions == null)
            return null;

        var validatorTypeDefn =
            GetDefaultValidatorType(validatorTypeName);

        if (validatorTypeDefn != null)
        {
            IDataValidator? validator = parsedOptions.Value.Deserialize(validatorTypeDefn, options) as IDataValidator;
            if (validator == null || string.IsNullOrEmpty(regionString))
                return null;

            return new DataRegionModel<IDataValidator>(regionString, validator);
        }

        return null;
    }

    private Type? GetDefaultValidatorType(string typeName)
    {
        if (_resolver.TryGetValue(typeName, out var type))
            return type;

        switch (typeName)
        {
            case nameof(SourceValidator):
                return typeof(SourceValidator);
            case nameof(NumberValidator):
                return typeof(NumberValidator);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DataRegionModel<IDataValidator> value,
        JsonSerializerOptions options)
    {
        var validatorTypeName = value.Value.GetType().Name;
        var type = GetDefaultValidatorType(validatorTypeName);
        if (type == null)
            throw new Exception(
                $"Could not write data validator type {validatorTypeName}. Ensure it is included in the validation resolver.");

        writer.WriteStartObject();
        writer.WriteString(JsonConstants.RangeReference, value.RegionString);
        writer.WriteString(JsonConstants.ClassType, validatorTypeName);
        writer.WritePropertyName(JsonConstants.Options);
        JsonSerializer.Serialize(writer, value.Value, type, options);
        writer.WriteEndObject();
    }
}