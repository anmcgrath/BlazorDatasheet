namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class FunctionParameterValidator
{
    /// <summary>
    /// Validates the parameters for a function.
    /// Note that the order is important.
    /// </summary>
    /// <param name="parameters"></param>
    /// <exception cref="InvalidFunctionDefinitionException">Throws an exception if the parameters are not valid.</exception>
    public void ValidateOrThrow(ParameterDefinition[] parameters)
    {
        if (!parameters.Any())
            return;

        var hasOptional = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (hasOptional && parameters[i].Requirement == ParameterRequirement.Required)
                throw new InvalidFunctionDefinitionException("Required parameters cannot be defined after optional.",
                    parameters[i].Name);
            if (parameters[i].IsRepeating && i != parameters.Length - 1)
                throw new InvalidFunctionDefinitionException(
                    "Repeating parameters must be defined as the last parameter", parameters[i].Name);

            hasOptional = hasOptional ||
                          parameters[i].Requirement == ParameterRequirement.Optional ||
                          parameters[i].IsRepeating;
        }
    }
}