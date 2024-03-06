using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class ParameterConverterTests
{
    [Test]
    public void Array_Conversion_To_NumberSequence()
    {
        var arr = CellValue.Array(new[] { new[] { CellValue.Number(2), CellValue.Logical(true) } });
        var env = new TestEnvironment();
        var converter = new ParameterConverter(env, new CellValueCoercer(env));
        var ns = converter.ConvertVal(arr, ParameterType.NumberSequence);
        ns
            .GetValue<CellValue[]>()!
            .Select(x => x.Data)
            .Should()
            .BeEquivalentTo(new[] { 2.0 });

        var ls = converter.ConvertVal(arr, ParameterType.LogicalSequence);
        ls
            .GetValue<CellValue[]>()!
            .Select(x => x.Data)
            .Should()
            .BeEquivalentTo(new[] { true, true });
    }
}