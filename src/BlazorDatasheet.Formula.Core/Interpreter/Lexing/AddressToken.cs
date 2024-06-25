using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class AddressToken : Token
{
    public Address Address { get; }

    public AddressToken(Address address, int positionStart) : base(Tag.AddressToken, positionStart)
    {
        Address = address;
    }
}