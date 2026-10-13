using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class AddressToken : Token
{
    public Address Address { get; }

    /// <summary>
    /// Whether the address was followed by '#', making it a spill-range reference (e.g. A1#).
    /// </summary>
    public bool IsSpillRange { get; init; }

    public AddressToken(Address address, int positionStart) : base(Tag.AddressToken, positionStart)
    {
        Address = address;
    }
}