using BlazorDatasheet.Formula.Core;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class CustomFunctionTests
{
    private FunctionParameterValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new();
    }


    [Test]
    public void Required_Args_After_Optional_Throws_Exception()
    {
        var defns = new ParameterDefinition[]
        {
            new ParameterDefinition("number_opt", ParameterType.Number,
                ParameterRequirement.Optional),
            new ParameterDefinition("number_opt", ParameterType.Number,
                ParameterRequirement.Required)
        };

        Assert.Throws<InvalidFunctionDefinitionException>(() => { _validator.ValidateOrThrow(defns); });
    }

    [Test]
    public void Repeat_Args_Defined_Before_End_Throws_Exception()
    {
        var defns = new ParameterDefinition[]
        {
            new ParameterDefinition("number_optional",
                ParameterType.Number,
                ParameterRequirement.Required,
                true),
            new ParameterDefinition("number_optiona1",
                ParameterType.Number,
                ParameterRequirement.Required,
                false),
        };

        Assert.Throws<InvalidFunctionDefinitionException>(() => { _validator.ValidateOrThrow(defns); });
    }

    [Test]
    public void Valid_Param_Definition_Does_Not_Throw_Exception()
    {
        var defns = new ParameterDefinition[]
        {
            new ParameterDefinition("number_required", ParameterType.Number,
                ParameterRequirement.Required),
            new ParameterDefinition("number_optional", ParameterType.Number,
                ParameterRequirement.Optional),
            new ParameterDefinition("number_repeating", ParameterType.Number,
                ParameterRequirement.Optional)
        };
        Assert.DoesNotThrow(() => { _validator.ValidateOrThrow(defns); });
    }
}

public class CustomFunctionDefinition : ISheetFunction
{
    private readonly ParameterDefinition[] _parameterDefinitions;

    public CustomFunctionDefinition(params ParameterDefinition[] parameterDefinitions)
    {
        _parameterDefinitions = parameterDefinitions;
    }

    public ParameterDefinition[] GetParameterDefinitions()
    {
        return _parameterDefinitions;
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        return CellValue.Empty;
    }

    public bool AcceptsErrors => false;
}