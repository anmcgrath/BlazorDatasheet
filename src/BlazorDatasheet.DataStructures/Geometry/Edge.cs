namespace BlazorDatasheet.DataStructures.Geometry;

[Flags]
public enum Edge : short
{
    None = 0,
    Top = 1,
    Right = 2,
    Left = 4,
    Bottom = 8,
}