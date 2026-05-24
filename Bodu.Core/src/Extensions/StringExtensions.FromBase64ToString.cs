// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.FromBase64ToString.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Decodes the Base64-encoded <paramref name="value" /> back into a string using <paramref name="encoding" /> (or
    /// <see cref="Encoding.UTF8" /> when <paramref name="encoding" /> is <see langword="null" />) to interpret the
    /// decoded bytes.
    /// </summary>
    /// <param name="value">The Base64-encoded input. Must not be <see langword="null" />.</param>
    /// <param name="encoding">
    /// The text encoding used to interpret the decoded bytes. Defaults to <see cref="Encoding.UTF8" /> when
    /// <see langword="null" />.
    /// </param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="value" /> does not contain valid Base64.
    /// </exception>
    /// <remarks>
    /// Named <c>FromBase64ToString</c> rather than <c>FromBase64String</c> to avoid colliding with the existing
    /// byte-returning helpers in <c>Bodu.Text.Encoding.BinaryEncodingExtensions</c>.
    /// </remarks>
    public static string FromBase64ToString(this string value, Encoding? encoding = null)
    {
        ThrowHelper.ThrowIfNull(value);

        Encoding effective = encoding ?? Encoding.UTF8;
        var bytes = Convert.FromBase64String(value);
        return effective.GetString(bytes);
    }
}
