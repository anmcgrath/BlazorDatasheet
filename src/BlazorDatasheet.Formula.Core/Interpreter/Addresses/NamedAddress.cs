namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class NamedAddress : Address
{
    public string Name { get; }

    public NamedAddress(string name) : base(AddressKind.NamedAddress)
    {
        Name = name;
    }
}