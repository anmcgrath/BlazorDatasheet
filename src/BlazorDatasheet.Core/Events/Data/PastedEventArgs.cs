using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Data;

public class PastedEventArgs
{
    public Sheet Sheet { get; }

    /// <summary>
    /// The region that was written.
    /// </summary>
    public IRegion Region { get; }

    public PasteMode Mode { get; }

    /// <summary>
    /// Whether the cells were moved (cut) rather than copied.
    /// </summary>
    public bool WasCut { get; }

    public PastedEventArgs(Sheet sheet, IRegion region, PasteMode mode, bool wasCut)
    {
        Sheet = sheet;
        Region = region;
        Mode = mode;
        WasCut = wasCut;
    }
}
