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
            throw new JsonException("A data validator must be a JSON object.");

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
                default:
                    reader.Skip();
                    break;
            }
        }

        if (string.IsNullOrEmpty(validatorTypeName))
            throw new JsonException("A serialized data validator must contain a Type property.");

        if (parsedOptions == null)
            throw new JsonException("A serialized data validator must contain an Options property.");

        if (string.IsNullOrEmpty(regionString))
            throw new JsonException("A serialized data validator must contain a Sqref property.");

        var validatorTypeDefn = GetDefaultValidatorType(validatorTypeName);
        if (validatorTypeDefn == null)
            throw new JsonException(
                $"Data validator type {validatorTypeName} is not registered in the data validation resolver.");

        var validator = parsedOptions.Value.Deserialize(validatorTypeDefn, options) as IDataValidator ??
                        throw new JsonException($"Could not deserialize data validator type {validatorTypeName}.");

        return new DataRegionModel<IDataValidator>(regionString, validator);
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
