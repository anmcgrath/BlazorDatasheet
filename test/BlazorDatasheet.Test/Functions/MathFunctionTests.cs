using System;
using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;
using FluentAssertions;
using NUnit.Framework;
using Parser = BlazorDatasheet.Formula.Core.Interpreter.Parsing.Parser;

namespace BlazorDatasheet.Test.Functions;

public class MathFunctionTests
{
	private TestEnvironment _env;

	[SetUp]
	public void Setup()
	{
		_env = new();
	}

	public object? Eval(string formulaString)
	{
		var eval = new Evaluator(_env);
		var parser = new Parser(_env);
		return eval.Evaluate(parser.FromString(formulaString)).Data;
	}

	[Test]
	public void Sin_Function_Tests()
	{
		_env.RegisterFunction(SinFunction.Descriptor);
		Eval("=sin(true)").Should().Be(Math.Sin(1));
		_env.SetCellValue(0, 0, true);
		Eval("=sin(A1)").Should().Be(Math.Sin(1));
		Eval("=sin(\"abc\")").Should().BeOfType(typeof(FormulaError));
		_env.SetCellValue(0, 0, "abc");
		Eval("=sin(A1)").Should().BeOfType(typeof(FormulaError));
		Eval("=sin(3.14)").Should().Be(Math.Sin(3.14));
		Eval("=sin(A3)").Should().Be(Math.Sin(0)); // empty
	}

	[Test]
	public void Sum_Function_Tests()
	{
		_env.RegisterFunction(SumFunction.Descriptor);
		var res1 = Eval("=sum(1, 2)");
		res1.Should().Be(3);
		Eval("=sum(1/0)").Should().BeOfType<FormulaError>();
		Eval("=sum(5)").Should().Be(5);
		Eval("=sum(true,true)").Should().Be(2);
		Eval("=sum(\"ab\",true)").Should().BeOfType<FormulaError>();
		Eval("=sum({1,2,3},4)").Should().Be(10);

		var nums = new double[] { 0.5, 1, 1.5, 2 };

		_env.SetCellValue(0, 0, nums[0]);
		_env.SetCellValue(1, 0, nums[1]);
		_env.SetCellValue(0, 2, nums[2]);
		_env.SetCellValue(1, 2, nums[3]);

		Eval("=sum(A1:A2,C1:C2)").Should().Be(nums.Sum());

		_env.SetCellValue(2, 1, 123);
		Eval("=sum(B3)").Should().Be(123);
	}

	[Test]
	public void Sum_With_True_Cell_Value_Should_Return_0()
	{
		_env.RegisterFunction(SumFunction.Descriptor);
		_env.SetCellValue(0, 0, true);
		Eval("=sum(A1)").Should().Be(0);
	}

	[Test]
	public void Sum_With_Text_Cell_Value_Should_Return_0()
	{
		// correct behaviour from excel - if sum range contains text it should be valuated as 0
		_env.RegisterFunction(SumFunction.Descriptor);
		_env.SetCellValue(0, 0, "abc");
		Eval("=sum(A1)").Should().Be(0);
	}

	[Test]
	public void Pow_Function_Tests()
	{
		_env.RegisterFunction(PowerFunction.Descriptor);
		Eval("=pow(5,true)").Should().Be(Math.Pow(5, 1));
		_env.SetCellValue(0, 0, true);
		Eval("=pow(5,A1)").Should().Be(Math.Pow(5, 1));
		Eval("=pow(\"abc\")").Should().BeOfType(typeof(FormulaError));
		_env.SetCellValue(0, 0, "abc");
		Eval("=pow(A1)").Should().BeOfType(typeof(FormulaError));
		Eval("=pow(2,4)").Should().Be(Math.Pow(2, 4));
		Eval("=pow(-2,-4)").Should().Be(Math.Pow(-2, -4));
		Eval("=pow(-2,4)").Should().Be(Math.Pow(-2, 4));
	}


	[Test]
	public void Intercept_Function_Tests()
	{
		_env.RegisterFunction(InterceptFunction.Descriptor);
		// ys
		_env.SetCellValue(0, 0, 1d);
		_env.SetCellValue(1, 0, 3d);
		_env.SetCellValue(2, 0, 4d);
		_env.SetCellValue(4, 0, 100d);

		// xs
		_env.SetCellValue(0, 1, 0d);
		_env.SetCellValue(1, 1, 1d);
		_env.SetCellValue(2, 1, 2d);
		_env.SetCellValue(4, 0, true);

		var intercept = Eval("=intercept(A1:A3,B1:B3)") as double?;
		intercept.Should().NotBeNull().And.BeApproximately(7 / 6d, 0.00001d);

		Eval("=intercept(A1:A4,B1:B3)").Should()
			.BeOfType<FormulaError>("The array number of rows are not the same.");
		Eval("=intercept(A1:B4,A1:A4)").Should()
			.BeOfType<FormulaError>("The array number of columns are not the same.");

		intercept = Eval("=intercept(A1:A5,B1:B5)") as double?;
		intercept.Should().NotBeNull().And.BeApproximately(7 / 6d, 0.00001d,
			because: "Row 4 col 0 value is skipped because it doesn't have a corresponding number value");
	}

	[Test]
	public void Slope_Function_Tests()
	{
		_env.RegisterFunction(SlopeFunction.Descriptor);
		// ys
		_env.SetCellValue(0, 0, 1d);
		_env.SetCellValue(1, 0, 3d);
		_env.SetCellValue(2, 0, 4d);
		_env.SetCellValue(4, 0, 100d);

		// xs
		_env.SetCellValue(0, 1, 0d);
		_env.SetCellValue(1, 1, 1d);
		_env.SetCellValue(2, 1, 2d);
		_env.SetCellValue(4, 0, true);

		var slope = Eval("=slope(A1:A3,B1:B3)") as double?;
		slope.Should().NotBeNull().And.BeApproximately(3 / 2d, 0.00001d);

		Eval("=slope(A1:A4,B1:B3)").Should()
			.BeOfType<FormulaError>("The array number of rows are not the same.");
		Eval("=slope(A1:B4,A1:A4)").Should()
			.BeOfType<FormulaError>("The array number of columns are not the same.");

		slope = Eval("=slope(A1:A5,B1:B5)") as double?;
		slope.Should().NotBeNull().And.BeApproximately(3 / 2d, 0.00001d,
			because: "Row 4 col 0 value is skipped because it doesn't have a corresponding number value");
	}

	[Test]
	public void Sqrt_Function_Tests()
	{
		_env.RegisterFunction(SqrtFunction.Descriptor);
		Eval("=sqrt(9)").Should().Be(3d);
		Eval("=sqrt(0)").Should().Be(0d);
		Eval("=sqrt(-1)").Should().BeOfType<FormulaError>();
		Eval("=sqrt(\"abc\")").Should().BeOfType<FormulaError>();
	}

	[Test]
	public void Log_Function_Tests()
	{
		_env.RegisterFunction(LogFunction.Descriptor);
		Eval("=log(100)").Should().Be(2d, "the base is 10 by default");
		(Eval("=log(8,2)") as double?).Should().NotBeNull().And.BeApproximately(3d, 0.00001d);
		Eval("=log(0)").Should().BeOfType<FormulaError>();
		Eval("=log(-1)").Should().BeOfType<FormulaError>();
		Eval("=log(10,0)").Should().BeOfType<FormulaError>();
		Eval("=log(10,1)").Should().BeOfType<FormulaError>();
	}

	[Test]
	public void Log10_Function_Tests()
	{
		_env.RegisterFunction(Log10Function.Descriptor);
		Eval("=log10(1000)").Should().Be(3d);
		Eval("=log10 (1000)").Should().Be(3d, "whitespace is allowed before a function's arguments");
		Eval("=log10(0)").Should().BeOfType<FormulaError>();
		Eval("=log10(-5)").Should().BeOfType<FormulaError>();
	}

	[Test]
	public void Abs_Function_Tests()
	{
		_env.RegisterFunction(AbsFunction.Descriptor);
		Eval("=abs(-3.5)").Should().Be(3.5d);
		Eval("=abs(3.5)").Should().Be(3.5d);
		Eval("=abs(0)").Should().Be(0d);
	}

	[Test]
	public void Sign_Function_Tests()
	{
		_env.RegisterFunction(SignFunction.Descriptor);
		Eval("=sign(12)").Should().Be(1d);
		Eval("=sign(-12)").Should().Be(-1d);
		Eval("=sign(0)").Should().Be(0d);
	}

	[Test]
	public void Degrees_And_Radians_Function_Tests()
	{
		_env.RegisterFunction(DegreesFunction.Descriptor);
		_env.RegisterFunction(RadiansFunction.Descriptor);

		(Eval("=degrees(3.14159265358979)") as double?).Should().NotBeNull()
			.And.BeApproximately(180d, 0.00001d);
		(Eval("=radians(180)") as double?).Should().NotBeNull()
			.And.BeApproximately(Math.PI, 0.00001d);
		(Eval("=degrees(radians(180))") as double?).Should().NotBeNull()
			.And.BeApproximately(180d, 0.00001d);
	}

	[Test]
	public void Mod_Function_Tests()
	{
		_env.RegisterFunction(ModFunction.Descriptor);
		Eval("=mod(5,3)").Should().Be(2d);
		Eval("=mod(-3,2)").Should().Be(1d, "the result takes the sign of the divisor, unlike c#");
		Eval("=mod(3,-2)").Should().Be(-1d, "the result takes the sign of the divisor, unlike c#");
		Eval("=mod(-3,-2)").Should().Be(-1d);
		Eval("=mod(1,0)").Should().BeOfType<FormulaError>();
	}

	[Test]
	public void Round_Function_Tests()
	{
		_env.RegisterFunction(RoundFunction.Descriptor);
		Eval("=round(2.4)").Should().Be(2d);
		Eval("=round(2.5)").Should().Be(3d, "midpoints round away from zero, not to even");
		Eval("=round(-2.5)").Should().Be(-3d);
		(Eval("=round(2.345,2)") as double?).Should().NotBeNull().And.BeApproximately(2.35d, 0.00001d);
		Eval("=round(1.005,2)").Should().Be(1.01d, "decimal midpoints must not be lost to binary scaling");
		Eval("=round(1234,-2)").Should().Be(1200d);
		Eval("=round(1234,-400)").Should().Be(0d);
	}

	[Test]
	public void SumSq_Function_Tests()
	{
		_env.RegisterFunction(SumSqFunction.Descriptor);
		Eval("=sumsq(3,4)").Should().Be(25d);

		_env.SetCellValue(0, 0, 1d);
		_env.SetCellValue(1, 0, 2d);
		_env.SetCellValue(2, 0, 3d);
		Eval("=sumsq(A1:A3)").Should().Be(14d);
		Eval("=sumsq(A1:A3,2)").Should().Be(18d);
	}

	[Test]
	public void Max_Function_Tests()
	{
		_env.RegisterFunction(MaxFunction.Descriptor);
		Eval("=max(1,7,3)").Should().Be(7d);
		Eval("=max(-5,-2)").Should().Be(-2d);

		_env.SetCellValue(0, 0, 4d);
		_env.SetCellValue(1, 0, 9d);
		_env.SetCellValue(2, 0, "abc");
		_env.SetCellValue(3, 0, true);
		Eval("=max(A1:A4)").Should().Be(9d, "text and logical values in a range are ignored");
		Eval("=max(A10:A11)").Should().Be(0d, "an empty range has a maximum of zero");
	}

	[Test]
	public void Min_Function_Tests()
	{
		_env.RegisterFunction(MinFunction.Descriptor);
		Eval("=min(4,1,7)").Should().Be(1d);
		Eval("=min(-5,-2)").Should().Be(-5d);

		_env.SetCellValue(0, 0, 4d);
		_env.SetCellValue(1, 0, 9d);
		_env.SetCellValue(2, 0, "abc");
		_env.SetCellValue(3, 0, true);
		Eval("=min(A1:A4)").Should().Be(4d, "text and logical values in a range are ignored");
		Eval("=min(A10:A11)").Should().Be(0d, "an empty range has a minimum of zero");
	}

	[Test]
	public void Pi_Function_Tests()
	{
		_env.RegisterFunction(PiFunction.Descriptor);
		Eval("=pi()").Should().Be(Math.PI);
	}

	[Test]
	public void Exp_Functin_Tests()
	{
		_env.RegisterFunction(ExpFunction.Descriptor);
		Eval("=exp(1)").Should().Be(Math.E);
		Eval("=exp(2)").Should().Be(Math.Pow(Math.E, 2));
	}
}