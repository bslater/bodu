// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.IsOneOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> equals any element of <paramref name="values" />
    /// under ordinal comparison.
    /// </summary>
    /// <param name="value">The candidate string. Must not be <see langword="null" />.</param>
    /// <param name="values">
    /// The candidate set. Must not be <see langword="null" />. Individual elements may be <see langword="null" /> and
    /// never match <paramref name="value" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when any element of <paramref name="values" /> equals <paramref name="value" /> under
    /// <see cref="StringComparison.Ordinal" />; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    public static bool IsOneOf(this string value, params string[] values)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(values);

        for (var i = 0; i < values.Length; i++)
        {
            if (string.Equals(value, values[i], StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> equals any element of <paramref name="values" />
    /// under the rule supplied by <paramref name="comparer" />.
    /// </summary>
    /// <param name="value">The candidate string. Must not be <see langword="null" />.</param>
    /// <param name="comparer">
    /// The equality comparer used to match <paramref name="value" /> against each element. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="values">The candidate set. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when any element of <paramref name="values" /> equals <paramref name="value" /> under
    /// <paramref name="comparer" />; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" />, <paramref name="comparer" />, or <paramref name="values" /> is
    /// <see langword="null" />.
    /// </exception>
    public static bool IsOneOf(this string value, IEqualityComparer<string> comparer, params string[] values)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(comparer);
        ThrowHelper.ThrowIfNull(values);

        for (var i = 0; i < values.Length; i++)
        {
            if (comparer.Equals(value, values[i])) return true;
        }

        return false;
    }
}
