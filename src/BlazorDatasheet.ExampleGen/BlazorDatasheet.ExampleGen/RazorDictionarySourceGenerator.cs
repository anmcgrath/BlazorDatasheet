using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazorDatasheet.ExampleGen;

/// <summary>
/// A sample source generator that creates C# classes based on the text file (in this case, Domain Driven Design ubiquitous language registry).
/// When using a simple text file as a baseline, we can create a non-incremental source generator.
/// </summary>
[Generator]
public class RazorDictionarySourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required for this generator.
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // If you would like to put some data to non-compilable file (e.g. a .txt file), mark it as an Additional File.

        // Go through all files marked as an Additional File in file properties.

        var builder = new StringBuilder();
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("namespace BlazorDatasheet.ExampleGen;");
        builder.AppendLine("public static class Sources {");
        builder.AppendLine("public static string GetSource(string componentName){");
        builder.AppendLine("return _sources[componentName];");
        builder.AppendLine("}");
        builder.AppendLine("private static Dictionary <string, string> _sources = new Dictionary<string, string>(){");


        foreach (var file in context.AdditionalFiles)
        {
            builder.AppendLine("// "+file.Path);

            if (file.Path.Contains($@"Components{Path.DirectorySeparatorChar}Examples"))
            {
                var i = file.Path.IndexOf(@$"Components{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
                var ns = file.Path.Substring(i, file.Path.Length - i)
                    .Replace(".razor", "")
                    .Replace($@"{Path.DirectorySeparatorChar}", ".");

                var source = file.GetText()?.ToString().Replace("\"", "\"\"");
                builder.AppendLine("{\"" + ns + "\", @\"" + source + "\"},");
            }
        }

        builder.AppendLine("};");
        builder.AppendLine("};");

        context.AddSource("Sources.g.cs", builder.ToString());
    }
}