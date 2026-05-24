// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.EqualsOrdinalIgnoreCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> equals <paramref name="other" /> under
    /// <see cref="StringComparison.OrdinalIgnoreCase" />.
    /// </summary>
    /// <param name="value">The receiver string; may be <see langword="null" />.</param>
    /// <param name="other">The other string; may be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when both strings are <see langword="null" />, or when neither is
    /// <see langword="null" /> and the characters compare equal under case-insensitive ordinal comparison; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Convenience shortcut for <c>string.Equals(value, other, StringComparison.OrdinalIgnoreCase)</c>.
    /// </remarks>
    public static bool EqualsOrdinalIgnoreCase(this string? value, string? other) =>
        string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
}
