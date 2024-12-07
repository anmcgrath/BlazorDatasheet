namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class NamedAddress : Address
{
    public string Name { get; }
    public bool IsValid { get; }

    public NamedAddress(string name, bool isValid) : base(AddressKind.NamedAddress)
    {
        Name = name;
        IsValid = isValid;
    }
}