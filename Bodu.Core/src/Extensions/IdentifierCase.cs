// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IdentifierCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Identifies which casing convention <see cref="StringExtensions.ToIdentifier(string, IdentifierCase)" /> applies to
/// the words detected in the source string after invalid identifier characters have been stripped.
/// </summary>
/// <remarks>
/// The detected words follow the same boundary rules as the other casing helpers (camelCase / PascalCase transitions,
/// digit transitions, and the standard separator set). Selecting <see cref="Preserve" /> keeps each word's existing
/// casing and joins them with no separator, which is the behaviour required when the source already obeys an identifier
/// convention and only invalid characters need to be removed.
/// </remarks>
public enum IdentifierCase
{
    /// <summary>
    /// Preserves each word's original casing and concatenates them with no separator.
    /// </summary>
    Preserve = 0,

    /// <summary>
    /// Lower-cases the first word and capitalises subsequent words (<c>userAccountId</c>).
    /// </summary>
    Camel = 1,

    /// <summary>
    /// Capitalises every word (<c>UserAccountId</c>).
    /// </summary>
    Pascal = 2,

    /// <summary>
    /// Lower-cases every word and joins with underscores (<c>user_account_id</c>).
    /// </summary>
    Snake = 3,
}
