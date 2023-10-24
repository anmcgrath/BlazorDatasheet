namespace BlazorDatasheet.Core.Validation;

public struct ValidationResult
{
    /// <summary>
    /// Validation messages for validators that resulted in an invalid result, if any.
    /// </summary>
    public List<string> FailMessages { get; }

    /// <summary>
    /// Whether the fail is "strict" in that it should not allow the value to be set.
    /// </summary>
    public bool IsStrictFail { get; }

    /// <summary>
    /// Whether the validation is valid after validators have been run.
    /// </summary>
    public bool IsValid { get; }

    public ValidationResult(List<string> failMessages, bool isStrictFail, bool isValid)
    {
        FailMessages = failMessages;
        IsStrictFail = isStrictFail;
        IsValid = isValid;
    }
}