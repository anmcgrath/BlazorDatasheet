// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Benchmarks;
using Benchmarks.Evaluator;
using Benchmarks.Lexer;
using Benchmarks.RangeEval;
using Benchmarks.SparseMatrixStore;

//BenchmarkRunner.Run<LexString>();
//BenchmarkRunner.Run<EvaluateExpression>();
//BenchmarkRunner.Run<RangeEvaluator>();
//BenchmarkRunner.Run<ReferenceEvaluator>();
BenchmarkRunner.Run<EvaluateFormulaExpressions>();