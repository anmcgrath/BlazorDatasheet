namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class ColAddress : Address
{
    public int ColIndex { get; }
    public string ColStr { get; }
    public bool IsFixed { get; }

    public ColAddress(int colIndex, string colStr, bool isFixed) : base(AddressKind.ColAddress)
    {
        ColIndex = colIndex;
        ColStr = colStr;
        IsFixed = isFixed;
    }
    
    public ColAddress(int colIndex, bool isFixed) : base(AddressKind.ColAddress)
    {
        ColIndex = colIndex;
        ColStr = RangeText.ColNumberToLetters(colIndex);
        IsFixed = isFixed;
    }
}