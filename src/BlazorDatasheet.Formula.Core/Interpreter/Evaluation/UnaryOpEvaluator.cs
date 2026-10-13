using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class UnaryOpEvaluator
{
    private readonly CellValueCoercer _cellValueCoercer;
    private readonly IEnvironment _environment;

    public UnaryOpEvaluator(CellValueCoercer cellValueCoercer, IEnvironment environment)
    {
        _cellValueCoercer = cellValueCoercer;
        _environment = environment;
    }

    public CellValue Evaluate(Tag opTag, CellValue value)
    {
        value = ArrayBroadcaster.Materialize(value, _environment);
        if (value.IsArray())
            return ArrayBroadcaster.Map(value, v => EvaluateScalar(opTag, v));

        return EvaluateScalar(opTag, value);
    }

    private CellValue EvaluateScalar(Tag opTag, CellValue value)
    {
        if (value.IsError())
            return value;

        switch (opTag)
        {
            case Tag.PlusToken:
                return EvaluatePlus(value);
            case Tag.MinusToken:
                return EvaluateMinus(value);
            case Tag.PercentToken:
                return EvaluatePercent(value);
            default:
                return CellValue.Error(ErrorType.Value);
        }
    }

    private CellValue EvaluatePercent(CellValue value)
    {
        var canConvert = _cellValueCoercer.TryCoerceNumber(value, out var convertedNum);
        if (canConvert)
            return CellValue.Number(convertedNum / 100d);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue EvaluateMinus(CellValue value)
    {
        var canConvert = _cellValueCoercer.TryCoerceNumber(value, out var convertedNum);
        if (canConvert)
            return CellValue.Number(-convertedNum);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue EvaluatePlus(CellValue value)
    {
        var canConvert = _cellValueCoercer.TryCoerceNumber(value, out var convertedNum);
        if (canConvert)
            return CellValue.Number(+convertedNum);
        return CellValue.Error(ErrorType.Value);
    }
}