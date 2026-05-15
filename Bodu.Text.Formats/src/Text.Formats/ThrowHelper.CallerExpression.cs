// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Bodu.Text.Formats.DotEnv;
using Bodu.Text.Formats.Ini;

namespace Bodu.Text.Formats;

internal static partial class ThrowHelper
{

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.BencodeFormatException_UnexpectedToken" />.</summary>
    private static readonly CompositeFormat s_unexpectedToken =
        CompositeFormat.Parse(FormatsResourceStrings.BencodeFormatException_UnexpectedToken);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.DotEnvDocument_KeyNotFound" />.</summary>
    private static readonly CompositeFormat s_dotEnvDocumentKeyNotFound =
        CompositeFormat.Parse(FormatsResourceStrings.DotEnvDocument_KeyNotFound);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.DotEnvFormatException_DuplicateKey" />.</summary>
    private static readonly CompositeFormat s_dotEnvDuplicateKey =
        CompositeFormat.Parse(FormatsResourceStrings.DotEnvFormatException_DuplicateKey);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.DotEnvFormatException_InvalidKey" />.</summary>
    private static readonly CompositeFormat s_dotEnvInvalidKey =
        CompositeFormat.Parse(FormatsResourceStrings.DotEnvFormatException_InvalidKey);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.DotEnvFormatException_MalformedEntry" />.</summary>
    private static readonly CompositeFormat s_dotEnvMalformedEntry =
        CompositeFormat.Parse(FormatsResourceStrings.DotEnvFormatException_MalformedEntry);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.DotEnvFormatException_UnterminatedDoubleQuote" />.</summary>
    private static readonly CompositeFormat s_dotEnvUnterminatedDoubleQuote =
        CompositeFormat.Parse(FormatsResourceStrings.DotEnvFormatException_UnterminatedDoubleQuote);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.DotEnvFormatException_UnterminatedSingleQuote" />.</summary>
    private static readonly CompositeFormat s_dotEnvUnterminatedSingleQuote =
        CompositeFormat.Parse(FormatsResourceStrings.DotEnvFormatException_UnterminatedSingleQuote);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.IniFormatException_DuplicateKey" />.</summary>
    private static readonly CompositeFormat s_iniDuplicateKey =
        CompositeFormat.Parse(FormatsResourceStrings.IniFormatException_DuplicateKey);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.IniFormatException_DuplicateSection" />.</summary>
    private static readonly CompositeFormat s_iniDuplicateSection =
        CompositeFormat.Parse(FormatsResourceStrings.IniFormatException_DuplicateSection);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.IniFormatException_GlobalKeyDisallowed" />.</summary>
    private static readonly CompositeFormat s_iniGlobalKeyDisallowed =
        CompositeFormat.Parse(FormatsResourceStrings.IniFormatException_GlobalKeyDisallowed);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.IniFormatException_MalformedSectionHeader" />.</summary>
    private static readonly CompositeFormat s_iniMalformedSectionHeader =
        CompositeFormat.Parse(FormatsResourceStrings.IniFormatException_MalformedSectionHeader);

    /// <summary>Cached parsed format for <see cref="FormatsResourceStrings.IniFormatException_MissingKey" />.</summary>
    private static readonly CompositeFormat s_iniMissingKey =
        CompositeFormat.Parse(FormatsResourceStrings.IniFormatException_MissingKey);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that a dictionary literal contained the same key
    /// twice.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the offending pairs.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentException_DuplicateDictionaryKey(string paramName) =>
        throw new ArgumentException(FormatsResourceStrings.ArgumentException_DuplicateDictionaryKey, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that a dictionary literal contained a
    /// <see langword="null" /> key.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the offending pairs.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentException_NullDictionaryKey(string paramName) =>
        throw new ArgumentException(FormatsResourceStrings.ArgumentException_NullDictionaryKey, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that a dictionary literal contained a
    /// <see langword="null" /> value.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the offending pairs.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentException_NullDictionaryValue(string paramName) =>
        throw new ArgumentException(FormatsResourceStrings.ArgumentException_NullDictionaryValue, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that a list literal contained a <see langword="null" />
    /// element.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the offending list.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentException_NullListElement(string paramName) =>
        throw new ArgumentException(FormatsResourceStrings.ArgumentException_NullListElement, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that the supplied stream does not support reading.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the stream.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentException_StreamNotReadable(string paramName) =>
        throw new ArgumentException(FormatsResourceStrings.ArgumentException_StreamNotReadable, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that the supplied stream does not support writing.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the stream.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentException_StreamNotWritable(string paramName) =>
        throw new ArgumentException(FormatsResourceStrings.ArgumentException_StreamNotWritable, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> indicating that a <see cref="BencodedValue" /> of an
    /// unsupported runtime kind was supplied.
    /// </summary>
    /// <param name="paramName">The name of the parameter that carried the offending value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowArgumentOutOfRange_UnsupportedBencodedValueType(string paramName) =>
        throw new ArgumentOutOfRangeException(paramName, FormatsResourceStrings.ArgumentOutOfRange_UnsupportedBencodedValueType);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that an integer value cannot be represented as
    /// an <see cref="long" />.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_IntegerOutOfRange() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_IntegerOutOfRange);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that an integer token contained no digits
    /// (e.g. <c>ie</c>, <c>i-e</c>).
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_InvalidInteger() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_InvalidInteger);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that an integer token contained leading zeros
    /// (e.g. <c>i03e</c>), which BEP 3 forbids.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_LeadingZerosInteger() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_LeadingZerosInteger);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that the input contained <c>i-0e</c>, which is
    /// invalid under BEP 3.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_NegativeZeroInteger() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_NegativeZeroInteger);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a dictionary entry's key was not a byte
    /// string.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_NonStringDictionaryKey() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_NonStringDictionaryKey);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a declared string length exceeds the bytes
    /// remaining in the input.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_StringLengthExceedsInput() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_StringLengthExceedsInput);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that the parser expected an ASCII digit at the
    /// start of a string length but found something else.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_StringLengthExpected() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_StringLengthExpected);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a multi-digit string length began with a
    /// zero (e.g. <c>04:spam</c>).
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_StringLengthLeadingZeros() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_StringLengthLeadingZeros);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a declared string length overflows
    /// <see cref="int.MaxValue" />.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_StringLengthTooLarge() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_StringLengthTooLarge);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a string length was not followed by the
    /// required <c>:</c> separator.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_StringMissingSeparator() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_StringMissingSeparator);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a decoded value was followed by unexpected
    /// trailing bytes.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_TrailingData() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_TrailingData);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that the input ended before the value being
    /// parsed was complete.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_UnexpectedEndOfData() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_UnexpectedEndOfData);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> identifying the unexpected byte and offset at which the
    /// parser stalled.
    /// </summary>
    /// <param name="token">The unexpected byte encountered in the input.</param>
    /// <param name="offset">The zero-based offset in the input at which the byte was encountered.</param>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_UnexpectedToken(byte token, int offset) =>
        throw new BencodeFormatException(
            string.Format(CultureInfo.InvariantCulture, s_unexpectedToken, (char)token, offset));

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that dictionary keys were either duplicated or
    /// not arranged in raw byte order.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_UnorderedDictionaryKeys() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_UnorderedDictionaryKeys);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a dictionary token (<c>d…e</c>) was not
    /// closed.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_UnterminatedDictionary() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_UnterminatedDictionary);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that an integer token (<c>i…e</c>) was not
    /// terminated by an <c>e</c>.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_UnterminatedInteger() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_UnterminatedInteger);

    /// <summary>
    /// Throws a <see cref="BencodeFormatException" /> indicating that a list token (<c>l…e</c>) was not closed.
    /// </summary>
    /// <exception cref="BencodeFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowBencodeFormatException_UnterminatedList() =>
        throw new BencodeFormatException(FormatsResourceStrings.BencodeFormatException_UnterminatedList);

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> when <paramref name="value" /> is <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The reference type being validated.</typeparam>
    /// <param name="value">The argument to validate.</param>
    /// <param name="paramName">
    /// The name of the parameter being validated. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <remarks>
    /// Forwards to <see cref="Bodu.ThrowHelper.ThrowIfNull{T}(T, string)" /> so callers in this project can use
    /// the unqualified name <c>ThrowHelper</c> uniformly. The forwarder exists to avoid the namespace shadowing
    /// that would otherwise break the existing <c>ThrowHelper.ThrowIfNull(...)</c> call sites once a project-local
    /// <c>ThrowHelper</c> is introduced in <see cref="Bodu.Text.Formats" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : class =>
        Bodu.ThrowHelper.ThrowIfNull(value, paramName);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that <see cref="System.Buffers.Text.Utf8Formatter" />
    /// failed to format an integer value into the destination buffer. Treated as a programmer error because the
    /// destination is always sized exactly.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowInvalidOperationException_IntegerFormatFailed() =>
        throw new InvalidOperationException(FormatsResourceStrings.InvalidOperationException_IntegerFormatFailed);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that <see cref="System.Buffers.Text.Utf8Formatter" />
    /// failed to format a string length into the destination buffer. Treated as a programmer error because the
    /// destination is always sized exactly.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowInvalidOperationException_StringLengthFormatFailed() =>
        throw new InvalidOperationException(FormatsResourceStrings.InvalidOperationException_StringLengthFormatFailed);

    /// <summary>
    /// Throws a <see cref="KeyNotFoundException" /> indicating that the specified key is absent from a
    /// <see cref="DotEnvDocument" />.
    /// </summary>
    /// <param name="key">The key that was not found.</param>
    /// <exception cref="KeyNotFoundException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowKeyNotFoundException_DotEnvDocument(string key) =>
        throw new KeyNotFoundException(
            string.Format(CultureInfo.InvariantCulture, s_dotEnvDocumentKeyNotFound, key));

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> indicating that the same key appeared more than once when
    /// duplicate keys are not permitted.
    /// </summary>
    /// <param name="key">The duplicate key name.</param>
    /// <param name="lineNumber">The 1-based line number on which the duplicate was encountered.</param>
    /// <exception cref="DotEnvFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowDotEnvFormatException_DuplicateKey(string key, int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.InvariantCulture, s_dotEnvDuplicateKey, key, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> indicating that a key name did not satisfy the required
    /// <c>[A-Za-z_][A-Za-z0-9_]*</c> pattern.
    /// </summary>
    /// <param name="key">The invalid key text that was encountered.</param>
    /// <param name="lineNumber">The 1-based line number on which the invalid key was encountered.</param>
    /// <exception cref="DotEnvFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowDotEnvFormatException_InvalidKey(string key, int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.InvariantCulture, s_dotEnvInvalidKey, key, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> indicating that a line could not be parsed as a valid
    /// <c>KEY=VALUE</c> assignment.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the malformed entry.</param>
    /// <exception cref="DotEnvFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowDotEnvFormatException_MalformedEntry(int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.InvariantCulture, s_dotEnvMalformedEntry, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> indicating that a double-quoted string was not closed before
    /// the end of the source.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number on which the double-quoted string opened.</param>
    /// <exception cref="DotEnvFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowDotEnvFormatException_UnterminatedDoubleQuote(int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.InvariantCulture, s_dotEnvUnterminatedDoubleQuote, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> indicating that a single-quoted string was not closed before
    /// the end of the line.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number on which the single-quoted string opened.</param>
    /// <exception cref="DotEnvFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowDotEnvFormatException_UnterminatedSingleQuote(int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.InvariantCulture, s_dotEnvUnterminatedSingleQuote, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> indicating that the same key appeared more than once in a
    /// section where duplicate keys are not permitted.
    /// </summary>
    /// <param name="key">The duplicate key name.</param>
    /// <param name="lineNumber">The 1-based line number on which the duplicate was encountered.</param>
    /// <exception cref="IniFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIniFormatException_DuplicateKey(string key, int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.InvariantCulture, s_iniDuplicateKey, key, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> indicating that the same section name appeared more than once
    /// when duplicate sections are not permitted.
    /// </summary>
    /// <param name="name">The duplicate section name.</param>
    /// <param name="lineNumber">The 1-based line number on which the duplicate was encountered.</param>
    /// <exception cref="IniFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIniFormatException_DuplicateSection(string name, int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.InvariantCulture, s_iniDuplicateSection, name, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> indicating that a key was encountered before the first section
    /// header when global entries are not permitted.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number on which the key was encountered.</param>
    /// <exception cref="IniFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIniFormatException_GlobalKeyDisallowed(int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.InvariantCulture, s_iniGlobalKeyDisallowed, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> indicating that a section header was malformed — either missing
    /// the closing bracket or containing an empty name.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the malformed header.</param>
    /// <exception cref="IniFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIniFormatException_MalformedSectionHeader(int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.InvariantCulture, s_iniMalformedSectionHeader, lineNumber), lineNumber);

    /// <summary>
    /// Throws an <see cref="IniFormatException" /> indicating that a key name was empty on a property line.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the offending entry.</param>
    /// <exception cref="IniFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIniFormatException_MissingKey(int lineNumber) =>
        throw new IniFormatException(
            string.Format(CultureInfo.InvariantCulture, s_iniMissingKey, lineNumber), lineNumber);

}
