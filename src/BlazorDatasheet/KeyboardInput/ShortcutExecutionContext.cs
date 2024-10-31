using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.KeyboardInput;

public class ShortcutExecutionContext
{
    public Datasheet Datasheet { get; }
    public Sheet Sheet { get; set; }
    public string Key { get; internal set; }
    public KeyboardModifiers Modifiers { get; internal set; }

    internal ShortcutExecutionContext(Datasheet datasheet, Sheet sheet)
    {
        Datasheet = datasheet;
        Sheet = sheet;
    }
}