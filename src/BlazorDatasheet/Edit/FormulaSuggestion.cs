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
    Name
}

/// <summary>
/// Something that is suggested for the name that is being typed into a formula.
/// </summary>
/// <param name="Name">The text that is suggested, and that is written into the formula.</param>
/// <param name="Kind">Whether the suggestion is a function or a name.</param>
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
/// <param name="Prefix">The start of the name, as it has been typed so far.</param>
/// <param name="Sheet">The sheet that the formula editor was given, if any.</param>
/// <param name="Defaults">
/// What is suggested when there is no provider: the variables and then the functions that start
/// with <paramref name="Prefix"/>.
/// </param>
public sealed record FormulaSuggestionRequest(
    string Prefix,
    Sheet? Sheet,
    IReadOnlyList<FormulaSuggestion> Defaults);

/// <summary>
/// Decides what a formula editor suggests. What it returns is shown as it is, so that a provider
/// can add to, remove from and reorder <see cref="FormulaSuggestionRequest.Defaults"/>.
/// It is given to an editor as a parameter, or to every editor inside a
/// <see cref="Microsoft.AspNetCore.Components.CascadingValue{TValue}"/> of this type.
/// </summary>
public delegate IEnumerable<FormulaSuggestion> FormulaSuggestionProvider(FormulaSuggestionRequest request);
