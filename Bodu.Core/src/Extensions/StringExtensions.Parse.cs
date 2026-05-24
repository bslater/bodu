// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Parses <paramref name="value" /> into the requested <typeparamref name="T" /> using the
    /// <see cref="IParsable{T}" /> contract and <see cref="CultureInfo.InvariantCulture" /> as the format provider.
    /// </summary>
    /// <typeparam name="T">The target type, which must implement <see cref="IParsable{T}" />.</typeparam>
    /// <param name="value">The string to parse. Must not be <see langword="null" />.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="value" /> is not in a valid format for <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when <paramref name="value" /> represents a value outside the range supported by
    /// <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// Provides a fluent <c>"42".Parse&lt;int&gt;()</c> form that flows from the string variable, in addition to the
    /// static <c>T.Parse</c> factory. The invariant culture is used by default to keep behaviour stable across machines
    /// and locales.
    /// </remarks>
    public static T Parse<T>(this string value)
        where T : IParsable<T>
    {
        ThrowHelper.ThrowIfNull(value);

        return T.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to parse <paramref name="value" /> into the requested <typeparamref name="T" /> using the
    /// <see cref="IParsable{T}" /> contract and <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <typeparam name="T">The target type, which must implement <see cref="IParsable{T}" />.</typeparam>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, contains the parsed value; otherwise the type's default value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> was parsed successfully; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static bool TryParse<T>(this string value, out T result)
        where T : IParsable<T>
    {
        ThrowHelper.ThrowIfNull(value);

        var ok = T.TryParse(value, CultureInfo.InvariantCulture, out T? parsed);
        result = parsed!;
        return ok;
    }

    /// <summary>
    /// Parses <paramref name="value" /> into the requested <typeparamref name="T" /> using the
    /// <see cref="ISpanParsable{T}" /> contract and <see cref="CultureInfo.InvariantCulture" />, routing through the
    /// underlying character span to avoid intermediate string allocation.
    /// </summary>
    /// <typeparam name="T">The target type, which must implement <see cref="ISpanParsable{T}" />.</typeparam>
    /// <param name="value">The string to parse. Must not be <see langword="null" />.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="value" /> is not in a valid format for <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when <paramref name="value" /> represents a value outside the range supported by
    /// <typeparamref name="T" />.
    /// </exception>
    public static T ParseSpan<T>(this string value)
        where T : ISpanParsable<T>
    {
        ThrowHelper.ThrowIfNull(value);

        return T.Parse(value.AsSpan(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to parse <paramref name="value" /> into the requested <typeparamref name="T" /> using the
    /// <see cref="ISpanParsable{T}" /> contract and <see cref="CultureInfo.InvariantCulture" />, routing through the
    /// underlying character span.
    /// </summary>
    /// <typeparam name="T">The target type, which must implement <see cref="ISpanParsable{T}" />.</typeparam>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, contains the parsed value; otherwise the type's default value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> was parsed successfully; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static bool TryParseSpan<T>(this string value, out T result)
        where T : ISpanParsable<T>
    {
        ThrowHelper.ThrowIfNull(value);

        var ok = T.TryParse(value.AsSpan(), CultureInfo.InvariantCulture, out T? parsed);
        result = parsed!;
        return ok;
    }
}
