namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class ColAddress : Address
{
    public int ColIndex { get; }
    public bool IsFixed { get; }
    
    public ColAddress(int colIndex, bool isFixed) : base(AddressKind.ColAddress)
    {
        ColIndex = colIndex;
        IsFixed = isFixed;
    }
}