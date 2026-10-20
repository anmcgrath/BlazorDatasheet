using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Edit;

public enum FormulaSuggestionKind
{
    /// <summary>
    /// A function, which is written with its opening bracket.
    /// </summary>
    Function,

    /// <summary>
    /// A variable, a named range or any other name, which is written as it is.
    /// </summary>
    Name,

    /// <summary>
    /// A table, which is written with its opening bracket, ready for a column or specifier e.g. Sales[
    /// </summary>
    Table,

    /// <summary>
    /// A column of a table, suggested inside the brackets of a structured reference. It is written escaped,
    /// followed by a closing bracket.
    /// </summary>
    TableColumn,

    /// <summary>
    /// A table specifier such as #Headers, suggested inside the brackets of a structured reference. It is written
    /// as it is, followed by a closing bracket.
    /// </summary>
    TableSpecifier
}

/// <summary>
/// Something that is suggested for the name that is being typed into a formula.
/// </summary>
/// <param name="Name">The text that is suggested, and that is written into the formula.</param>
/// <param name="Kind">What sort of thing is suggested, which decides how it is written into the formula.</param>
/// <param name="Description">Shown under the suggestion when it is selected.</param>
/// <param name="Detail">Shown beside the name, e.g. the formula that a name stands for.</param>
public sealed record FormulaSuggestion(
    string Name,
    FormulaSuggestionKind Kind,
    string? Description = null,
    string? Detail = null);

/// <summary>
/// What suggestions are asked for.
/// </summary>
/// <param name="Prefix">
/// The start of the name, as it has been typed so far. Inside the brackets of a structured reference, this is the
/// start of the column or specifier e.g. "Am" for Sales[Am
/// </param>
/// <param name="Sheet">The sheet that the formula editor was given, if any.</param>
/// <param name="Defaults">
/// What is suggested when there is no provider: the variables and then the functions that start
/// with <paramref name="Prefix"/>. Inside the brackets of a structured reference, the columns of the table and
/// the table specifiers.
/// </param>
public sealed record FormulaSuggestionRequest(
    string Prefix,
    Sheet? Sheet,
    IReadOnlyList<FormulaSuggestion> Defaults)
{
    /// <summary>
    /// Whether the caret is inside the brackets of a structured reference, e.g. Sales[Am
    /// </summary>
    public bool IsInStructuredReference { get; init; }

    /// <summary>
    /// The table of the structured reference that the caret is inside, if it is known.
    /// </summary>
    public string? TableName { get; init; }
}

/// <summary>
/// Decides what a formula editor suggests. What it returns is shown as it is, so that a provider
/// can add to, remove from and reorder <see cref="FormulaSuggestionRequest.Defaults"/>.
/// It is given to an editor as a parameter, or to every editor inside a
/// <see cref="Microsoft.AspNetCore.Components.CascadingValue{TValue}"/> of this type.
/// </summary>
public delegate IEnumerable<FormulaSuggestion> FormulaSuggestionProvider(FormulaSuggestionRequest request);
