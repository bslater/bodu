// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Numerics;

public readonly partial struct Complex<T> :
    IFormattable,
    ISpanFormattable
{
    /// <summary>
    /// Returns the default string representation of this complex value.
    /// </summary>
    /// <returns>The value formatted as <c>&lt;real; imaginary&gt;</c>.</returns>
    public override string ToString() =>
        Format(null, null);

    /// <summary>
    /// Returns a string representation of this complex value, applying the specified format to each component.
    /// </summary>
    /// <param name="format">The numeric format specifier applied to the real and imaginary components.</param>
    /// <returns>The value formatted as <c>&lt;real; imaginary&gt;</c>.</returns>
    public string ToString(string? format) =>
        Format(format, null);

    /// <summary>
    /// Returns a string representation of this complex value, applying the specified format and culture to each
    /// component.
    /// </summary>
    /// <param name="format">The numeric format specifier applied to the real and imaginary components.</param>
    /// <param name="formatProvider">The culture used to render the numeric components.</param>
    /// <returns>The value formatted as <c>&lt;real; imaginary&gt;</c>.</returns>
    /// <remarks>
    /// The semicolon component separator is fixed regardless of culture so that the format does not collide with the
    /// decimal comma used by cultures such as <c>de-DE</c> and <c>fr-FR</c>, matching
    /// <see cref="System.Numerics.Complex" />.
    /// </remarks>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Format(format, formatProvider);

    /// <summary>
    /// Attempts to format this complex value into the provided character span.
    /// </summary>
    /// <param name="destination">The span that receives the formatted characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="format">The numeric format specifier applied to each component.</param>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns><see langword="true" /> if formatting succeeded; otherwise, <see langword="false" />.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string text = Format(format.IsEmpty ? null : format.ToString(), provider);
        if (text.Length <= destination.Length)
        {
            text.CopyTo(destination);
            charsWritten = text.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary>
    /// Formats this complex value as <c>&lt;real; imaginary&gt;</c>, rendering each component with the supplied format
    /// and culture.
    /// </summary>
    /// <param name="format">The numeric format specifier applied to each component.</param>
    /// <param name="provider">The culture used to render the numeric components.</param>
    /// <returns>The formatted string representation.</returns>
    private string Format(string? format, IFormatProvider? provider)
    {
        var builder = new StringBuilder();
        builder.Append('<');
        builder.Append(((IFormattable)Real).ToString(format, provider));
        builder.Append("; ");
        builder.Append(((IFormattable)Imaginary).ToString(format, provider));
        builder.Append('>');
        return builder.ToString();
    }
}
