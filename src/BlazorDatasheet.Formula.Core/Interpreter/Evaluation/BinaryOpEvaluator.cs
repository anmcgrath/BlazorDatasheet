using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class BinaryOpEvaluator
{
    private readonly CellValueCoercer _cellValueCoercer;

    public BinaryOpEvaluator(CellValueCoercer cellValueCoercer)
    {
        _cellValueCoercer = cellValueCoercer;
    }

    public CellValue Evaluate(CellValue left, Tag op, CellValue right)
    {
        if (left.IsError())
            return left;
        if (right.IsError())
            return right;


        switch (op)
        {
            case Tag.PlusToken:
                return EvaluateAdd(left, right);
            case Tag.StarToken:
                return EvaluateMultiply(left, right);
            case Tag.MinusToken:
                return EvaluateSubtract(left, right);
            case Tag.SlashToken:
                return EvaluateDivide(left, right);
            case Tag.NotEqualToToken:
                return EvaluateNotEqual(left, right);
            case Tag.EqualsToken:
                return EvaluateEqual(left, right);
            case Tag.GreaterThanToken:
                return EvaluateGreaterThan(left, right);
            case Tag.GreaterThanOrEqualToToken:
                return EvaluateGreaterThanOrEqualTo(left, right);
            case Tag.LessThanToken:
                return EvaluateLessThan(left, right);
            case Tag.LessThanOrEqualToToken:
                return EvaluateLessThanOreEqualTo(left, right);
        }

        return CellValue.Error(new FormulaError(ErrorType.Na));
    }

    private CellValue EvaluateLessThanOreEqualTo(CellValue left, CellValue right)
    {
        if (left.Data == null || right.Data == null)
            return CellValue.Logical(false);

        var compareResult = ((IComparable)left.Data).CompareTo(right.Data);
        return CellValue.Logical(compareResult <= 0);
    }

    private CellValue EvaluateLessThan(CellValue left, CellValue right)
    {
        if (left.Data == null || right.Data == null)
            return CellValue.Logical(false);

        var compareResult = ((IComparable)left.Data).CompareTo(right.Data);
        return CellValue.Logical(compareResult < 0);
    }

    private CellValue EvaluateGreaterThanOrEqualTo(CellValue left, CellValue right)
    {
        if (left.Data == null || right.Data == null)
            return CellValue.Logical(false);

        var compareResult = ((IComparable)left.Data).CompareTo(right.Data);
        return CellValue.Logical(compareResult >= 0);
    }

    private CellValue EvaluateGreaterThan(CellValue left, CellValue right)
    {
        if (left.Data == null || right.Data == null)
            return CellValue.Logical(false);

        var compareResult = ((IComparable)left.Data).CompareTo(right.Data);
        return CellValue.Logical(compareResult > 0);
    }

    private CellValue EvaluateEqual(CellValue left, CellValue right)
    {
        var equal = left.IsEqualTo(right);
        return CellValue.Logical(left.IsEqualTo(right));
    }

    private CellValue EvaluateNotEqual(CellValue left, CellValue right)
    {
        return CellValue.Logical(!left.IsEqualTo(right));
    }

    public CellValue EvaluateDivide(CellValue left, CellValue right)
    {
        var numPair = CoercedPair.AsNumbers(left, right, _cellValueCoercer);
        if (numPair.HasError)
            return CellValue.Error(ErrorType.Value);

        if (numPair.Value2 == 0)
            return CellValue.Error(ErrorType.Div0);

        return CellValue.Number(numPair.Value1 / numPair.Value2);
    }

    public CellValue EvaluateAdd(CellValue left, CellValue right)
    {
        var numPair = CoercedPair.AsNumbers(left, right, _cellValueCoercer);
        if (numPair.HasError)
            return CellValue.Error(ErrorType.Value);

        if (numPair.Value2 == 0)
            return CellValue.Error(ErrorType.Div0);

        return CellValue.Number(numPair.Value1 + numPair.Value2);
    }

    public CellValue EvaluateSubtract(CellValue left, CellValue right)
    {
        var numPair = CoercedPair.AsNumbers(left, right, _cellValueCoercer);
        if (numPair.HasError)
            return CellValue.Error(ErrorType.Value);

        if (numPair.Value2 == 0)
            return CellValue.Error(ErrorType.Div0);

        return CellValue.Number(numPair.Value1 - numPair.Value2);
    }


    public CellValue EvaluateMultiply(CellValue left, CellValue right)
    {
        var numPair = CoercedPair.AsNumbers(left, right, _cellValueCoercer);
        if (numPair.HasError)
            return CellValue.Error(ErrorType.Value);

        if (numPair.Value2 == 0)
            return CellValue.Error(ErrorType.Div0);

        return CellValue.Number(numPair.Value1 * numPair.Value2);
    }
}