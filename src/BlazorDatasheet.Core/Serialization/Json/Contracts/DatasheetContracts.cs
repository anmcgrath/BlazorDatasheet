using System.Collections;
using System.Text.Json.Serialization.Metadata;

namespace BlazorDatasheet.Core.Serialization.Json.Contracts;

internal class DatasheetContracts
{
    public static void IgnoreEmptyArray(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                property.ShouldSerialize = (_, list) => list != null && ((IList)list).Count > 0;
            }
        }
    }
}