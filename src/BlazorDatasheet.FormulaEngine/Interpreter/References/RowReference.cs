using ExpressionEvaluator.CodeAnalysis.Types;

namespace BlazorDatasheet.FormulaEngine.Interpreter.References;

public class RowReference : Reference
{
    public int RowNumber { get; }
    public bool IsAbsoluteReference { get; }

    public RowReference(int rowNumber, bool isAbsoluteReference)
    {
        RowNumber = rowNumber;
        IsAbsoluteReference = isAbsoluteReference;
    }

    public override ReferenceKind Kind => ReferenceKind.Row;

    public override string ToRefText()
    {
        return (IsAbsoluteReference ? "$" : "") + (RowNumber + 1);
    }

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == Kind &&
               ((RowReference)reference).RowNumber == RowNumber;
    }
}