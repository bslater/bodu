// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriter.WriteProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Toml.Writer;

public ref partial struct Utf8TomlWriter
{
    /// <summary>
    /// The strict UTF-8 encoding used to decode UTF-8 name and value overloads; invalid byte sequences are rejected
    /// rather than replaced.
    /// </summary>
    private static readonly UTF8Encoding s_utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Writes the name of the table key whose value follows.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WritePropertyName(ReadOnlySpan<char> name) =>
        WritePropertyName(name.ToString());

    /// <summary>
    /// Writes the name of the table key whose value follows from UTF-8 text.
    /// </summary>
    /// <param name="utf8Name">The UTF-8 key text.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="utf8Name" /> is not valid UTF-8.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or the key has already been written to the current table.
    /// </exception>
    public readonly void WritePropertyName(ReadOnlySpan<byte> utf8Name) =>
        WritePropertyName(DecodeUtf8(utf8Name));

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete or the enclosing table has no pending property name.
    /// </exception>
    public readonly void WriteString(ReadOnlySpan<char> value) =>
        WriteString(value.ToString());

    /// <summary>
    /// Writes a string value from UTF-8 text.
    /// </summary>
    /// <param name="utf8Value">The UTF-8 string value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="utf8Value" /> is not valid UTF-8.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete or the enclosing table has no pending property name.
    /// </exception>
    public readonly void WriteString(ReadOnlySpan<byte> utf8Value) =>
        WriteString(DecodeUtf8(utf8Value));

    /// <summary>
    /// Writes a property name and the start of its table as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when opening the table would exceed the configured maximum nesting depth.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteStartTable(string name)
    {
        WritePropertyName(name);
        WriteStartTable();
    }

    /// <summary>
    /// Writes a property name and the start of its array as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="TomlSerializationException">
    /// Thrown when opening the array would exceed the configured maximum nesting depth.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteStartArray(string name)
    {
        WritePropertyName(name);
        WriteStartArray();
    }

    /// <summary>
    /// Writes a property name and string value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> or <paramref name="value" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteString(string name, string value)
    {
        WritePropertyName(name);
        WriteString(value);
    }

    /// <summary>
    /// Writes a property name and 64-bit signed integer value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The integer value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteInteger(string name, long value)
    {
        WritePropertyName(name);
        WriteInteger(value);
    }

    /// <summary>
    /// Writes a property name and IEEE 754 binary64 floating-point value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The floating-point value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteFloat(string name, double value)
    {
        WritePropertyName(name);
        WriteFloat(value);
    }

    /// <summary>
    /// Writes a property name and Boolean value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The Boolean value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteBoolean(string name, bool value)
    {
        WritePropertyName(name);
        WriteBoolean(value);
    }

    /// <summary>
    /// Writes a property name and offset date-time value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The offset date-time value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteOffsetDateTime(string name, DateTimeOffset value)
    {
        WritePropertyName(name);
        WriteOffsetDateTime(value);
    }

    /// <summary>
    /// Writes a property name and local date-time value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The local date-time value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteLocalDateTime(string name, DateTime value)
    {
        WritePropertyName(name);
        WriteLocalDateTime(value);
    }

    /// <summary>
    /// Writes a property name and local date value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The local date value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteLocalDate(string name, DateOnly value)
    {
        WritePropertyName(name);
        WriteLocalDate(value);
    }

    /// <summary>
    /// Writes a property name and local time value as a pair.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The local time value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document is already complete, the innermost open container is not a table, another property name
    /// is already pending, or <paramref name="name" /> has already been written to the current table.
    /// </exception>
    public readonly void WriteLocalTime(string name, TimeOnly value)
    {
        WritePropertyName(name);
        WriteLocalTime(value);
    }

    /// <summary>
    /// Decodes UTF-8 text supplied to a name or value overload, rejecting invalid sequences.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 bytes to decode.</param>
    /// <param name="paramName">The name of the parameter being decoded.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="utf8Text" /> is not valid UTF-8.</exception>
    private static string DecodeUtf8(ReadOnlySpan<byte> utf8Text, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(utf8Text))] string? paramName = null)
    {
        try
        {
            return s_utf8Strict.GetString(utf8Text);
        }
        catch (DecoderFallbackException ex)
        {
            throw new ArgumentException(TomlResourceStrings.Arg_Invalid_TomlInvalidUtf8, paramName, ex);
        }
    }
}
