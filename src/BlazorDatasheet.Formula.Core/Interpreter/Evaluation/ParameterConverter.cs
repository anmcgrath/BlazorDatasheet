using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

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
    /// Performs an implicit conversion of a value into another type, specified by <paramref name="type"/>
    /// See the below link as we try to follow this standard.
    /// http://docs.oasis-open.org/office/v1.2/os/OpenDocument-v1.2-os-part2.html
    /// If the conversion cannot be performed, an error is usually the result.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public CellValue ConvertVal(CellValue value, ParameterType type)
    {
        if (value.IsCellReference() && type.IsScalar())
            value = GetCellReferenceValue(value);

        if (value.IsError())
            return value;

        switch (type)
        {
            case ParameterType.Any:
                return value;
            case ParameterType.Number:
                return ToNumber(value);
            case ParameterType.NumberSequence:
                return ToNumberSequence(value);
            case ParameterType.Logical:
                return ToLogical(value);
            case ParameterType.LogicalSequence:
                return ToLogicalSequence(value);
            case ParameterType.Text:
                return ToText(value);
            case ParameterType.Date:
                return ToDate(value);
            case ParameterType.Array:
                return ToArray(value);
            case ParameterType.Integer:
                return ToInteger(value);
            default:
                return CellValue.Error(ErrorType.Value);
        }
    }

    private CellValue ToInteger(CellValue value)
    {
        var asNumber = ToNumber(value);
        if (asNumber.IsError())
            return asNumber;

        return CellValue.Number(Convert.ToInt32(asNumber.NumberValue));
    }

    private CellValue ToLogicalSequence(CellValue value)
    {
        if (value.ValueType == CellValueType.Reference)
            return CellValue.Sequence(ToLogicalSequence(_environment.GetNonEmptyInRange((Reference)value.Data!)));
        if (value.ValueType == CellValueType.Array)
            return CellValue.Sequence(ToLogicalSequence(Flatten((CellValue[][])value.Data!)));

        return CellValue.Sequence(new[] { ToLogical(value) });
    }

    private CellValue ToNumberSequence(CellValue value)
    {
        if (value.ValueType == CellValueType.Reference)
            return CellValue.Sequence(ToNumberSequence(_environment.GetNonEmptyInRange((Reference)value.Data!)));
        if (value.ValueType == CellValueType.Array)
            return CellValue.Sequence(ToNumberSequence(Flatten((CellValue[][])value.Data!)));

        return CellValue.Sequence(new[] { ToNumber(value) });
    }

    private static IEnumerable<CellValue> Flatten(CellValue[][] values)
    {
        foreach (var row in values)
            foreach (var value in row)
                yield return value;
    }

    /// <summary>
    /// Keeps the numbers and errors of a range. A range read from the sheet is already a list, so
    /// it is counted first and the result is sized exactly, rather than growing a list and copying it.
    /// </summary>
    private CellValue[] ToNumberSequence(IEnumerable<CellValue> values)
    {
        if (values is IReadOnlyList<CellValue> list)
        {
            var count = 0;
            for (var i = 0; i < list.Count; i++)
            {
                var type = list[i].ValueType;
                if (type == CellValueType.Number || type == CellValueType.Error)
                    count++;
            }

            if (count == 0)
                return Array.Empty<CellValue>();

            var result = new CellValue[count];
            var n = 0;
            for (var i = 0; i < list.Count; i++)
            {
                var val = list[i];
                if (val.ValueType == CellValueType.Number || val.ValueType == CellValueType.Error)
                    result[n++] = val;
            }

            return result;
        }

        var results = new List<CellValue>();
        foreach (var val in values)
        {
            if (val.ValueType == CellValueType.Error)
                results.Add(val);
            else if (val.ValueType == CellValueType.Number)
                results.Add(ToNumber(val));
        }

        return results.ToArray();
    }

    private CellValue[] ToLogicalSequence(IEnumerable<CellValue> values)
    {
        var results = new List<CellValue>();
        foreach (var val in values)
        {
            if (val.ValueType == CellValueType.Logical || val.ValueType == CellValueType.Error)
                results.Add(val);
            else if (val.ValueType == CellValueType.Number)
                results.Add(ToLogical(val));
        }

        return results.ToArray();
    }

    private CellValue ToArray(CellValue value)
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
                return CellValue.Array(new[]
                    { new[] { _environment.GetCellValue(c.RowIndex, c.ColIndex, c.SheetName) } });
            }

            if (r.Kind == ReferenceKind.Range)
            {
                return CellValue.Array(_environment.GetRangeValues(r));
            }
        }

        return CellValue.Array(new[] { new[] { value } });
    }

    private CellValue ToDate(CellValue value)
    {
        var coerceDate = _cellValueCoercer.TryCoerceDate(value, out var val);
        if (coerceDate)
            return CellValue.Date(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue ToNumber(CellValue value)
    {
        var coerceNum = _cellValueCoercer.TryCoerceNumber(value, out var val);
        if (coerceNum)
            return CellValue.Number(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue ToLogical(CellValue value)
    {
        var corceBool = _cellValueCoercer.TryCoerceBool(value, out var val);
        if (corceBool)
            return CellValue.Logical(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue ToText(CellValue value)
    {
        var coerceText = _cellValueCoercer.TryCoerceString(value, out var val);
        if (coerceText)
            return CellValue.Text(val);
        return CellValue.Error(ErrorType.Value);
    }

    private CellValue GetCellReferenceValue(CellValue value)
    {
        var reference = ((Reference)value.Data!);
        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return _environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex, cellRef.SheetName);
        }

        return CellValue.Error(ErrorType.Na);
    }
}