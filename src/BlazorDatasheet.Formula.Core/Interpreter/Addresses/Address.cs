namespace BlazorDatasheet.Formula.Core.Interpreter.Addresses;

public class Address
{
    public AddressKind Kind { get; private set; }

    public Address(AddressKind kind)
    {
        Kind = kind;
    }
}