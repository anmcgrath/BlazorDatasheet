using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class ExpFunction
{
	private static readonly ParameterDefinition[] Parameters =
	[
		new("number", ParameterType.Number, ParameterRequirement.Required,
			description: "The number to raise e to"),
	];

	public static FunctionDescriptor Descriptor { get; } = new(
		name: "EXP",
		parameterDefinitions: Parameters,
		invoker: Evaluate,
		acceptsErrors: false,
		isVolatile: false,
		description: "Returns e raised to the power of a given number");

	private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
	{
		return CellValue.Number(System.Math.Pow(System.Math.E, args[0].GetValue<double>()));
	}
}