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
    /// Performs an implicit conversion of a value into another type, specified by <paramref name="definition"/>
    /// See the below link as we try to follow this standard.
    /// http://docs.oasis-open.org/office/v1.2/os/OpenDocument-v1.2-os-part2.html
    /// If the conversion cannot be performed, an error is usually the result.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="definition"></param>
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

        return CellValue.Number(Convert.ToInt32(asNumber.Data!));
    }

    private CellValue ToLogicalSequence(CellValue value)
    {
        CellValue[]? values = null;

        if (value.ValueType == CellValueType.Reference)
            values = _environment.GetNonEmptyInRange((Reference)value.Data!).ToArray();
        else if (value.ValueType == CellValueType.Array)
            values = value.GetValue<CellValue[][]>()!.SelectMany(x => x).ToArray();

        if (values == null) return CellValue.Sequence(new[] { ToLogical(value) });

        var results = new List<CellValue>();
        foreach (var val in values)
        {
            if (val.IsEmpty)
                continue;
            if (val.ValueType == CellValueType.Logical || val.ValueType == CellValueType.Error)
                results.Add(val);
            else if (val.ValueType == CellValueType.Number)
                results.Add(ToLogical(val));
        }

        return CellValue.Sequence(results.ToArray());
    }

    private bool IsLogicalOrError(CellValue value)
    {
        return value.ValueType == CellValueType.Logical ||
               value.ValueType == CellValueType.Error;
    }

    private CellValue ToNumberSequence(CellValue value)
    {
        CellValue[]? values = null;

        if (value.ValueType == CellValueType.Reference)
            values = _environment.GetNonEmptyInRange((Reference)value.Data!).ToArray();
        else if (value.ValueType == CellValueType.Array)
            values = value.GetValue<CellValue[][]>()!.SelectMany(x => x).ToArray();

        if (values == null) return CellValue.Sequence(new[]
        {
            ToNumber(value)
        });

        var results = new List<CellValue>();
        foreach (var val in values)
        {
            if (val.IsEmpty)
                continue;
            if (val.ValueType == CellValueType.Error)
                results.Add(val);
            else if (val.ValueType == CellValueType.Number)
                results.Add(ToNumber(val));
        }

        return CellValue.Sequence(results.ToArray());
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
                return CellValue.Array(new[] { new[] { _environment.GetCellValue(c.RowIndex, c.ColIndex) } });
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
            return _environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex);
        }

        return CellValue.Error(ErrorType.Na);
    }
}