// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Creates the serialization exceptions thrown by the shared serializer source (see
/// <c>Bodu.Text.Serialization/shared/</c>). Each Bodu text-format package defines the same factory surface over its own
/// exception type and resource strings, so shared converters raise failures without naming either — this implementation
/// stamps the reader's byte offset into every exception, matching the Bencode diagnostics contract.
/// </summary>
internal static class SerializationThrowHelper
{
    /// <summary>
    /// Creates the exception reporting that the current token is not the string token a converter requires.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BencodeSerializationException ExpectedString(ref Utf8BencodeReader reader) =>
        new(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedByteString, reader.TokenType),
            reader.BytesConsumed);

    /// <summary>
    /// Creates the exception reporting that the current token is not the integer token a converter requires.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BencodeSerializationException ExpectedInteger(ref Utf8BencodeReader reader) =>
        new(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType),
            reader.BytesConsumed);

    /// <summary>
    /// Creates the exception reporting that a string does not name a member of the target enumeration.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <param name="text">The unmatched enumeration text.</param>
    /// <param name="enumType">The target enumeration type.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BencodeSerializationException EnumValueNotFound(ref Utf8BencodeReader reader, string text, Type enumType) =>
        new(
            string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_EnumValueNotFound, text, enumType),
            reader.BytesConsumed);
}
