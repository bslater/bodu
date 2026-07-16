// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization;
#elif TOML
namespace Bodu.Text.Toml.Serialization;
#endif

/// <summary>
/// Creates the serialization exceptions thrown by the shared serializer source. The factory surface is format-neutral;
/// the format-specific branches preserve each format's pinned diagnostics contract — Bencode stamps the offending
/// token's start offset into every exception, whereas TOML carries the message only because the enclosing converter
/// stamps the source position and member path during unwind.
/// </summary>
internal static class SerializationThrowHelper
{
    /// <summary>
    /// Creates the exception reporting that the current token is not the string token a converter requires.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormatSerializationException ExpectedString(ref FormatReader reader) =>
#if BENCODE
        new(
            string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ExpectedByteString, reader.TokenType),
            reader.TokenStartIndex);
#else
        new(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));
#endif

    /// <summary>
    /// Creates the exception reporting that the current token is not the integer token a converter requires.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormatSerializationException ExpectedInteger(ref FormatReader reader) =>
#if BENCODE
        new(
            string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType),
            reader.TokenStartIndex);
#else
        new(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));
#endif

    /// <summary>
    /// Creates the exception reporting that a string does not name a member of the target enumeration.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <param name="text">The unmatched enumeration text.</param>
    /// <param name="enumType">The target enumeration type.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormatSerializationException EnumValueNotFound(ref FormatReader reader, string text, Type enumType) =>
#if BENCODE
        new(
            string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_EnumValueNotFound, text, enumType),
            reader.TokenStartIndex);
#else
        new(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_EnumValueNotFound, text, enumType));
#endif
}
