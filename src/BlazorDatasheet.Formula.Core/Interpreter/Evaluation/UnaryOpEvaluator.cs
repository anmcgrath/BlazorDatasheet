using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class UnaryOpEvaluator
{
    private readonly CellValueCoercer _cellValueCoercer;

    public UnaryOpEvaluator(CellValueCoercer cellValueCoercer)
    {
        _cellValueCoercer = cellValueCoercer;
    }

    public CellValue Evaluate(Tag opTag, CellValue value)
    {
        switch (opTag)
        {
            case Tag.PlusToken:
                return EvaluatePlus(value);
            case Tag.MinusToken:
                return EvaluteMinus(value);
            default:
                return CellValue.Error(ErrorType.Value);
        }
    }

    private CellValue EvaluteMinus(CellValue value)
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