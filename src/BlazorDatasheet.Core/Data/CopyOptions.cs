namespace BlazorDatasheet.Core.Data;

public class CopyOptions
{
    public bool CopyFormula { get; set; } = true;
    public bool CopyValues { get; set; } = true;
    public bool CopyFormat { get; set; } = true;

    public static CopyOptions DefaultCopyOptions => new CopyOptions();
}