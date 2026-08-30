namespace BlazorDatasheet.Core.Serialization;

/// <summary>
/// Marks a type as one that should never be written when a workbook is serialized.
/// Currently honoured for conditional formats.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class SerializationIgnoreAttribute : Attribute
{
}
