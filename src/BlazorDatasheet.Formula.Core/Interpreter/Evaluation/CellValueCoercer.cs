using BlazorDatasheet.Formula.Core.Extensions;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class CellValueCoercer
{
    private readonly IEnvironment _environment;

    public CellValueCoercer(IEnvironment environment)
    {
        _environment = environment;
    }

    public bool TryCoerceNumber(CellValue cellValue, out double val)
    {
        if (cellValue.IsEmpty)
        {
            val = 0;
            return true;
        }

        if (cellValue.ValueType == CellValueType.Reference)
        {
            var reference = cellValue.GetValue<Reference>()!;
            if (reference.Kind == ReferenceKind.Range)
            {
                val = double.NaN;
                return false;
            }

            if (reference.Kind == ReferenceKind.Cell)
            {
                var cellRef = (CellReference)reference;
                return TryCoerceNumber(_environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex),
                    out val);
            }

            val = double.NaN;
            return false;
        }

        if (cellValue.ValueType == CellValueType.Number)
        {
            val = (double)cellValue.Data!;
            return true;
        }

        if (cellValue.ValueType == CellValueType.Date)
        {
            val = ((DateTime)cellValue.Data!).ToNumber();
            return true;
        }

        if (cellValue.ValueType == CellValueType.Logical)
        {
            val = ((bool)cellValue.Data!) ? 1 : 0;
            return true;
        }

        if (cellValue.ValueType == CellValueType.Text)
        {
            if (double.TryParse((string)cellValue.Data!, out var parsedDouble))
            {
                val = parsedDouble;
                return true;
            }

            val = double.NaN;
            return false;
        }

        val = double.NaN;
        return false;
    }

    public bool TryCoerceBool(CellValue cellValue, out bool val)
    {
        val = false;

        if (cellValue.ValueType == CellValueType.Reference)
        {
            var reference = cellValue.GetValue<Reference>()!;
            if (reference.Kind == ReferenceKind.Range)
            {
                return false;
            }

            if (reference.Kind == ReferenceKind.Cell)
            {
                var cellRef = (CellReference)reference;
                return TryCoerceBool(_environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex),
                    out val);
            }

            return false;
        }


        if (cellValue.IsEmpty)
            return false;

        if (cellValue.ValueType == CellValueType.Logical)
        {
            val = (bool)cellValue.Data!;
            return true;
        }

        if (cellValue.ValueType == CellValueType.Number)
        {
            val = ((double)cellValue.Data!) != 0;
            return true;
        }

        if (cellValue.ValueType == CellValueType.Text)
        {
            if (bool.TryParse(cellValue.Data?.ToString(), out var parsedBool))
            {
                val = parsedBool;
                return true;
            }
        }

        return false;
    }

    public bool TryCoerceString(CellValue cellValue, out string str)
    {
        str = string.Empty;

        if (cellValue.ValueType == CellValueType.Reference)
        {
            var reference = cellValue.GetValue<Reference>()!;
            if (reference.Kind == ReferenceKind.Range)
            {
                return false;
            }

            if (reference.Kind == ReferenceKind.Cell)
            {
                var cellRef = (CellReference)reference;
                return TryCoerceString(_environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex),
                    out str);
            }

            return false;
        }

        if (cellValue.Data == null)
            str = string.Empty;
        else
            str = cellValue.Data.ToString()!;

        return true;
    }

    public bool TryCoerceDate(CellValue cellValue, out DateTime dateTime)
    {
        dateTime = DateTime.UnixEpoch;

        if (cellValue.ValueType == CellValueType.Reference)
        {
            var reference = cellValue.GetValue<Reference>()!;
            if (reference.Kind == ReferenceKind.Range)
            {
                return false;
            }

            if (reference.Kind == ReferenceKind.Cell)
            {
                var cellRef = (CellReference)reference;
                return TryCoerceDate(_environment.GetCellValue(cellRef.RowIndex, cellRef.ColIndex),
                    out dateTime);
            }

            return false;
        }

        if (cellValue.ValueType == CellValueType.Date)
        {
            dateTime = (DateTime)cellValue.Data!;
            return true;
        }

        if (cellValue.ValueType == CellValueType.Number)
        {
            var num = (double)cellValue.Data!;
            dateTime = new DateTime(1900, 1, 1).AddDays(num);
        }

        if (cellValue.ValueType == CellValueType.Text)
        {
            var s = (string)cellValue.Data!;
            if (DateTime.TryParse(s, out var parsedDateTime))
            {
                dateTime = parsedDateTime;
                return true;
            }
        }

        return false;
    }
}