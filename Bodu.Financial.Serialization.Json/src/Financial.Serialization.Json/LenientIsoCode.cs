// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LenientIsoCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Normalizes the ISO currency codes that the converters read under <see cref="FinancialJsonPolicy.Lenient" />.
/// </summary>
internal static class LenientIsoCode
{
    /// <summary>
    /// Returns <paramref name="code" /> without its surrounding white space, with its ASCII lower-case letters raised
    /// to upper case and every other character unchanged.
    /// </summary>
    /// <param name="code">The code as read from the payload.</param>
    /// <returns>The normalized code, which the caller then validates as an ISO code.</returns>
    /// <remarks>
    /// Invariant uppercasing would also map U+017F LATIN SMALL LETTER LONG S to <c>S</c>, so a code that holds it would
    /// read as the ISO code it resembles. Raising <c>a</c>-<c>z</c> alone leaves such a code to fail validation, as any
    /// other code that is not three ASCII letters does.
    /// </remarks>
    internal static string Normalize(string code)
    {
        string trimmed = code.Trim();

        return string.Create(trimmed.Length, trimmed, static (destination, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                destination[i] = char.IsAsciiLetterLower(c) ? (char)(c - ('a' - 'A')) : c;
            }
        });
    }
}
