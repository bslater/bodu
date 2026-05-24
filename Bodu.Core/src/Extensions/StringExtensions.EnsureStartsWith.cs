// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.EnsureStartsWith.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> guaranteed to begin with <paramref name="prefix" />, prepending the prefix
    /// only when it is not already present.
    /// </summary>
    /// <param name="value">The string to qualify. Must not be <see langword="null" />.</param>
    /// <param name="prefix">The prefix that the result must begin with. Must not be <see langword="null" />.</param>
    /// <param name="comparison">
    /// The comparison rule used to test whether <paramref name="value" /> already begins with
    /// <paramref name="prefix" />. Defaults to <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when it already starts with <paramref name="prefix" /> under
    /// <paramref name="comparison" />; otherwise a new string equal to <paramref name="prefix" /> concatenated with
    /// <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="prefix" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "api/users".EnsureStartsWith("/");   // "/api/users"
    /// "/api/users".EnsureStartsWith("/");  // "/api/users" (prefix already present)
    ///]]>
    /// </code>
    /// </example>
    public static string EnsureStartsWith(
        this string value,
        string prefix,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(prefix);

        return value.StartsWith(prefix, comparison) ? value : prefix + value;
    }
}
