namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ParsingContext
{
    public string CallingSheetName { get; }
    public bool ExplicitSheetNameReference { get; }

    public ParsingContext(string callingSheetName, bool explicitSheetNameReference)
    {
        CallingSheetName = callingSheetName;
        ExplicitSheetNameReference = explicitSheetNameReference;
    }
}