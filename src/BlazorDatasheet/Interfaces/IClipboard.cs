using BlazorDatasheet.Data;

namespace BlazorDatasheet.Interfaces;

public interface IClipboard
{
    public Task Copy(IRegion region, Sheet sheet);
}