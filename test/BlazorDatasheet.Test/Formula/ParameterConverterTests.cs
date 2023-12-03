using System.Collections.Generic;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class ParameterConverterTests
{
    private ParameterTypeConverter _toArgConverter;

    [SetUp]
    public void Setup()
    {
        _toArgConverter = new(new TestEnvironment());
    }

    [Test]
    public void Scalar_Param_Converts_To_Simple_Number()
    {
        var paramDef = new ParameterDefinition(
            "test",
            ParameterType.Number,
            ParameterRequirement.Required,
            false);

        _toArgConverter.ConvertVal(2, paramDef).Should().BeEquivalentTo(new CellValue(2));
    }
}