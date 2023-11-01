using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Interfaces;

public interface IWriteableCell
{
    public CellFormat Format { set; }
    public string Type { set; }
    public string? Formula { set; }
    public object? Value { set; }
    void SetMetaData(string name, object? value);
}