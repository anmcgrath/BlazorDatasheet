using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json.Converters;

internal class DataValidationJsonConverter : JsonConverter<DataRegionModel<IDataValidator>>
{
    private readonly Func<string, Type?> _dataValidationTypeResolver;

    public DataValidationJsonConverter(Func<string, Type?> dataValidationTypeResolver)
    {
        _dataValidationTypeResolver = dataValidationTypeResolver;
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
                case "sqref":
                    regionString = reader.GetString();
                    break;
                case "type":
                    validatorTypeName = reader.GetString();
                    break;
                case "options":
                    parsedOptions = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(validatorTypeName))
            return null;

        if (parsedOptions == null)
            return null;

        var validatorTypeDefn =
            GetDefaultValidatorType(validatorTypeName) ?? _dataValidationTypeResolver(validatorTypeName);

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
        // Default CFs 
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
        var type = GetDefaultValidatorType(validatorTypeName) ?? _dataValidationTypeResolver(validatorTypeName);
        if (type == null)
            throw new Exception(
                $"Could not write data validator type {validatorTypeName}. Ensure it is included in the validation resolver.");

        writer.WriteStartObject();
        writer.WriteString("sqref", value.RegionString);
        writer.WriteString("type", validatorTypeName);
        writer.WritePropertyName("options");
        JsonSerializer.Serialize(writer, value.Value, type, options);
        writer.WriteEndObject();
    }
}