namespace BlazorDatasheet.Interfaces;

public interface IClipboard
{
    public Task WriteTextAsync(string text);
}