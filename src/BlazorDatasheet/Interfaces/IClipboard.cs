namespace BlazorDatasheet.Interfaces;

public interface IClipboard
{
    public Task<string> ReadTextAsync();
    public Task WriteTextAsync(string text);
}