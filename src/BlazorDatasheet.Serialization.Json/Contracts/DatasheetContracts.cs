using System.Collections;
using System.Text.Json.Serialization.Metadata;

namespace BlazorDatasheet.Serialization.Json.Contracts;

public class DatasheetContracts
{
    public static void IgnoreEmptyArray(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                property.ShouldSerialize = (object _, object? list) => list != null && ((IList)list).Count > 0;
            }
        }
    }
}