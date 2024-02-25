using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class ReferenceToken : Token
{
    public Reference Reference { get; }

    public ReferenceToken(Reference reference, int positionStart) : base(Tag.ReferenceToken, positionStart)
    {
        Reference = reference;
    }
}