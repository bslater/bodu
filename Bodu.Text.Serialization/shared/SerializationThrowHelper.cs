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
/// the format-specific branches preserve each format's pinned diagnostics contract - Bencode stamps the offending
/// token's start offset into every exception raised on read, whereas TOML carries the message only because the
/// enclosing converter stamps the source position and member path during unwind. An exception raised on write, where
/// there is no token, carries the message only in both formats.
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

    /// <summary>
    /// Creates the exception reporting that a string holds an integer, which the converter of the target enumeration
    /// does not allow.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <param name="text">The enumeration text that holds an integer.</param>
    /// <param name="enumType">The target enumeration type.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormatSerializationException EnumIntegerNotAllowed(
        ref FormatReader reader,
        string text,
        Type enumType) =>
#if BENCODE
        new(
            string.Format(
                CultureInfo.CurrentCulture,
                FormatResourceStrings.Op_Invalid_EnumIntegerNotAllowed,
                text,
                enumType),
            reader.TokenStartIndex);
#else
        new(
            string.Format(
                CultureInfo.CurrentCulture,
                FormatResourceStrings.Op_Invalid_EnumIntegerNotAllowed,
                text,
                enumType));
#endif

    /// <summary>
    /// Creates the exception reporting that an enumeration value is neither a member nor a combination of flags, which
    /// a converter that does not allow integer values cannot write.
    /// </summary>
    /// <param name="text">The decimal text the runtime formats the value as.</param>
    /// <param name="enumType">The enumeration type.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormatSerializationException EnumValueNotWritable(string text, Type enumType) =>
        new(
            string.Format(
                CultureInfo.CurrentCulture,
                FormatResourceStrings.Op_Invalid_EnumValueNotWritable,
                text,
                enumType));

    /// <summary>
    /// Creates the exception reporting that the integer token holds a value outside the range of the target type, an
    /// integer type or the underlying type of an enumeration.
    /// </summary>
    /// <param name="reader">The reader positioned on the offending token.</param>
    /// <param name="value">The integer the token holds.</param>
    /// <param name="targetType">The target integer or enumeration type.</param>
    /// <param name="innerException">The exception the checked conversion threw, if any.</param>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormatSerializationException IntegerOutOfRange(
        ref FormatReader reader,
        object value,
        Type targetType,
        Exception? innerException = null) =>
#if BENCODE
        new(
            string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_IntegerOverflow, value, targetType),
            reader.TokenStartIndex,
            innerException);
#else
        new(
            string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_IntegerOverflow, value, targetType),
            innerException);
#endif
}
