using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

/// <summary>
/// A structured reference to part of a table, e.g. Sales[Amount] or [@Qty]. <see cref="Token.Text"/> is
/// the text as it was written, including the table name.
/// </summary>
public class StructuredReferenceToken : Token
{
    public StructuredReferenceDescriptor Descriptor { get; }

    public StructuredReferenceToken(string text, StructuredReferenceDescriptor descriptor, int positionStart)
        : base(Tag.StructuredReferenceToken, text, positionStart)
    {
        Descriptor = descriptor;
    }
}
