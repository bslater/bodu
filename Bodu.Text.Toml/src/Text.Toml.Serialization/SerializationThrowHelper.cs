// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Creates the serialization exceptions thrown by the shared serializer source (see
/// <c>Bodu.Text.Serialization/shared/</c>). Each Bodu text-format package defines the same factory surface over its own
/// exception type and resource strings, so shared converters raise failures without naming either — this
/// implementation carries the message only, matching the TOML diagnostics contract in which the enclosing converter
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
    internal static TomlSerializationException ExpectedString(ref TomlDocumentReader reader) =>
        new(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedString, reader.TokenType));

    /// <summary>
    /// Creates the exception reporting that the current token is not the integer token a converter requires.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TomlSerializationException ExpectedInteger(ref TomlDocumentReader reader) =>
        new(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));

    /// <summary>
    /// Creates the exception reporting that a string does not name a member of the target enumeration.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <param name="text">The unmatched enumeration text.</param>
    /// <param name="enumType">The target enumeration type.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TomlSerializationException EnumValueNotFound(ref TomlDocumentReader reader, string text, Type enumType) =>
        new(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_EnumValueNotFound, text, enumType));
}
