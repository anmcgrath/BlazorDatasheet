namespace BlazorDatasheet.Core.Data;

/// <summary>
/// The position and data of a <see cref="HeadingGroup"/>.
/// </summary>
public record HeadingGroupInfo(int Start, int End, HeadingGroup Group)
{
    public string Label => Group.Label;
}
