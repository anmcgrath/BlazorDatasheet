using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter2.Evaluation;

public class ParameterConverter
{
    private readonly IEnvironment _environment;
    private readonly CellValueCoercer _cellValueCoercer;

    public ParameterConverter(IEnvironment environment, CellValueCoercer cellValueCoercer)
    {
        _environment = environment;
        _cellValueCoercer = cellValueCoercer;
    }

    /// <summary>
    /// Performs an implicit conversion of a value into another type, specified by <paramref name="definition"/>
    /// See the below link as we try to follow this standard.
    /// http://docs.oasis-open.org/office/v1.2/os/OpenDocument-v1.2-os-part2.html
    /// If the conversion cannot be performed, an error is usually the result.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public CellValue ConvertVal(CellValue value, ParameterDefinition definition)
    {
        if (value.IsError())
            return value;

        switch (definition.Type)
        {
            case ParameterType.Any:
                return value;
            case ParameterType.Number:
                return ToNumber(value, definition);
            case ParameterType.NumberSequence:
                return ToNumberSequence(value, definition);
            case ParameterType.Logical:
                return ToLogical(value, definition);
            case ParameterType.LogicalSequence:
                return ToLogicalSequence(value, definition);
            case ParameterType.Text:
                return ToText(value, definition);
            case ParameterType.Date:
                return ToDate(value, definition);
            case ParameterType.Array:
                return ToArray(value, definition);
            default:
                return CellValue.Error(ErrorType.Value);
        }
    }

    private CellValue ToLogicalSequence(CellValue value, ParameterDefinition definition)
    {
        if (value.ValueType == CellValueType.Reference)
        {
            List<CellValue> results = new List<CellValue>();
            var range = _environment.GetRangeValues(value.GetValue<Reference>()!);
            for (int i = 0; i < range.Length; i++)
            {
                for (int j = 0; j < range[i].Length; j++)
                {
                    if (range[i][j].ValueType == CellValueType.Logical ||
                        range[i][j].ValueType == CellValueType.Error)
                    {
                        results.Add(range[i][j]);
                    }

                    if (range[i][j].ValueType == CellValueType.Number)
                        results.Add(ToLogical(range[i][j], definition));
                }
            }

            return CellValue.Sequence(results.ToArray());
        }

        if (value.ValueType == CellValueType.Array)
        {
            var array = value.GetValue<CellValue[][]>()!;
            var cellValueLogOrError = array.SelectMany(x => x)
                .Where(y => y.ValueType == CellValueType.Logical || y.ValueType == CellValueType.Error);
            return CellValue.Sequence(cellValueLogOrError.ToArray());
        }

        return CellValue.Sequence(new[] { ToLogical(value, definition) });
    }

    private CellValue ToNumberSequence(CellValue value, ParameterDefinition definition)
    {
        if (value.ValueType == CellValueType.Reference)
        {
            List<CellValue> results = new List<CellValue>();
            var valAsREf = (Reference)value.Data!;
            var range = _environment.GetRangeValues(valAsREf);
            for (int i = 0; i < range.Length; i++)
            {
                for (int j = 0; j < range[i].Length; j++)
                {
                    if (range[i][j].ValueType == CellValueType.Number ||
                        range[i][j].ValueType == CellValueType.Error)
                    {
                        results.Add(range[i][j]);
                    }
                }
            }

            return CellValue.Sequence(results.ToArray());
        }

        if (value.ValueType == CellValueType.Array)
        {
            var array = value.GetValue<CellValue[][]>()!;
            var vals = new List<CellValue>();
            foreach (var row in array)
            {
                foreach (var col in row)
                {
                    if (col.ValueType == CellValueType.Number || col.IsError())
                        vals.Add(col);
                }
            }

            var cellValueNumOrErr =
                array.SelectMany(x => x)
                    .Where(y => y.ValueType == CellValueType.Number || y.ValueType == CellValueType.Error);
            return CellValue.Sequence(cellValueNumOrErr.ToArray());
        }

        // single value
        return CellValue.Sequence(new[] { ToNumber(value, definition) });
    }

    private CellValue ToArray(CellValue value, ParameterDefinition definition)
    {
        if (value.IsEmpty)
            return CellValue.Array(Array.Empty<CellValue[]>());

        if (value.ValueType == CellValueType.Array)
            return value;

        if (value.ValueType == CellValueType.Reference)
        {
            var r = (Reference)value.Data!;
            if (r.Kind == ReferenceKind.Cell)
            {
                var c = (CellReference)r;
                return CellValue.Array(new[] { new[] { _environment.GetCellValue(c.Row.RowNumber, c.Col.ColNumber) } });
            }

            if (r.Kind == ReferenceKind.Range)
            {
                return CellValue.Array(_environment.GetRangeValues(r));
            }
        }

        return CellValue.Array(new[] { new[] { value } });
    }

    private CellValue ToDate(CellValue value, ParameterDefinition definition)
    {
        var coerceDate = _cellValueCoercer.TryCoerceDate(value, out var val);
        if (coerceDate)
            return CellValue.Date(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue ToNumber(CellValue value, ParameterDefinition definition)
    {
        var coerceNum = _cellValueCoercer.TryCoerceNumber(value, out var val);
        if (coerceNum)
            return CellValue.Number(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue ToLogical(CellValue value, ParameterDefinition definition)
    {
        var corceBool = _cellValueCoercer.TryCoerceBool(value, out var val);
        if (corceBool)
            return CellValue.Logical(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue ToText(CellValue value, ParameterDefinition definition)
    {
        var coerceText = _cellValueCoercer.TryCoerceString(value, out var val);
        if (coerceText)
            return CellValue.Text(val);
        return CellValue.Error(ErrorType.Value);
    }
}