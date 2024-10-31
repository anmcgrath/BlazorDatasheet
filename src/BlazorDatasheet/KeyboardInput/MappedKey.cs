namespace BlazorDatasheet.KeyboardInput;

internal record struct MappedKey
{
    public KeyboardModifiers Modifiers { get; set; }
    public string Key { get; set; }

    public MappedKey(string key, KeyboardModifiers modifiers)
    {
        Modifiers = modifiers;
        Key = key;
    }
}