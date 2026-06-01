// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToBase64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns the Base64-encoded representation of <paramref name="value" /> after encoding it with
    /// <paramref name="encoding" /> (or <see cref="Encoding.UTF8" /> when <paramref name="encoding" /> is
    /// <see langword="null" />).
    /// </summary>
    /// <param name="value">The string to encode. Must not be <see langword="null" />.</param>
    /// <param name="encoding">
    /// The text encoding used to convert <paramref name="value" /> to bytes before Base64 encoding. Defaults to
    /// <see cref="Encoding.UTF8" /> when <see langword="null" />.
    /// </param>
    /// <returns>The Base64-encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Convenience wrapper over <see cref="Convert.ToBase64String(byte[])" /> that avoids the boilerplate of converting
    /// the string to bytes manually. Pair with <see cref="FromBase64ToString(string, Encoding)" /> for the inverse
    /// operation.
    /// </remarks>
    public static string ToBase64(this string value, Encoding? encoding = null)
    {
        ThrowHelper.ThrowIfNull(value);

        Encoding effective = encoding ?? Encoding.UTF8;
        var bytes = effective.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }
}
