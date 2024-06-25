using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class BinaryOpEvaluator
{
    private readonly CellValueCoercer _cellValueCoercer;
    private readonly IEnvironment _environment;

    public BinaryOpEvaluator(CellValueCoercer cellValueCoercer, IEnvironment environment)
    {
        _cellValueCoercer = cellValueCoercer;
        _environment = environment;
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
                return EvaluateLessThanOrEqualTo(left, right);
            case Tag.ColonToken:
                return EvaluateRangeOperator(left, right);
            case Tag.AmpersandToken:
                return EvaluateConcatOperator(left, right);
        }

        return CellValue.Error(new FormulaError(ErrorType.Na));
    }

    private CellValue EvaluateConcatOperator(CellValue left, CellValue right)
    {
        var parseLeftStr = _cellValueCoercer.TryCoerceString(left, out var leftStr);
        if (!parseLeftStr)
            return CellValue.Error(ErrorType.Value);

        var parseRightStr = _cellValueCoercer.TryCoerceString(right, out var rightStr);
        if (!parseRightStr)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Text(leftStr + rightStr);
    }

    private CellValue EvaluateRangeOperator(CellValue left, CellValue right)
    {
        if (left.ValueType != CellValueType.Reference || right.ValueType != CellValueType.Reference)
            return CellValue.Error(ErrorType.Na);

        var leftRef = (Reference)left.Data!;
        var rightRef = (Reference)right.Data!;

        var regJoined = leftRef.Region.GetBoundingRegion(rightRef.Region);
        var c1 = new CellAddress(regJoined.Top, regJoined.Left);
        var c2 = new CellAddress(regJoined.Bottom, regJoined.Right);

        return CellValue.Reference(new RangeReference(c1, c2));
    }

    private CellValue EvaluateLessThan(CellValue left, CellValue right)
    {
        left = EvalCellValueIfItIsReference(left);
        right = EvalCellValueIfItIsReference(right);
        return CellValue.Logical(left.IsLessThan(right));
    }

    private CellValue EvaluateLessThanOrEqualTo(CellValue left, CellValue right)
    {
        left = EvalCellValueIfItIsReference(left);
        right = EvalCellValueIfItIsReference(right);
        return CellValue.Logical(left.IsLessThanOrEqualTo(right));
    }

    private CellValue EvaluateGreaterThan(CellValue left, CellValue right)
    {
        left = EvalCellValueIfItIsReference(left);
        right = EvalCellValueIfItIsReference(right);
        return CellValue.Logical(left.IsGreaterThan(right));
    }

    private CellValue EvaluateGreaterThanOrEqualTo(CellValue left, CellValue right)
    {
        left = EvalCellValueIfItIsReference(left);
        right = EvalCellValueIfItIsReference(right);
        return CellValue.Logical(left.IsGreaterThanOrEqualTo(right));
    }

    private CellValue EvaluateEqual(CellValue left, CellValue right)
    {
        left = EvalCellValueIfItIsReference(left);
        right = EvalCellValueIfItIsReference(right);
        return CellValue.Logical(left.IsEqualTo(right));
    }

    private CellValue EvaluateNotEqual(CellValue left, CellValue right)
    {
        left = EvalCellValueIfItIsReference(left);
        right = EvalCellValueIfItIsReference(right);
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

        return CellValue.Number(numPair.Value1 + numPair.Value2);
    }

    public CellValue EvaluateSubtract(CellValue left, CellValue right)
    {
        var numPair = CoercedPair.AsNumbers(left, right, _cellValueCoercer);
        if (numPair.HasError)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Number(numPair.Value1 - numPair.Value2);
    }


    public CellValue EvaluateMultiply(CellValue left, CellValue right)
    {
        var numPair = CoercedPair.AsNumbers(left, right, _cellValueCoercer);
        if (numPair.HasError)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Number(numPair.Value1 * numPair.Value2);
    }

    private CellValue EvalCellValueIfItIsReference(CellValue value)
    {
        if (value.IsCellReference())
        {
            var cellRef = (CellReference)value.Data!;
            return _environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex);
        }

        return value;
    }
}