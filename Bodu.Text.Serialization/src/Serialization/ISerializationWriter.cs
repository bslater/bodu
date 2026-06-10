// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ISerializationWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides a forward-only sink that a <see cref="FormatConverter{T}" /> writes format-neutral value tokens to. Each
/// format supplies an implementation that materializes the tokens as its own document, so a converter written against
/// this interface targets any format.
/// </summary>
/// <remarks>
/// <para>
/// The contract mirrors <see cref="System.Text.Json.Utf8JsonWriter" />: objects are bracketed by
/// <see cref="WriteStartObject" /> and <see cref="WriteEndObject" />, arrays by <see cref="WriteStartArray" /> and
/// <see cref="WriteEndArray" />, and each property is a <see cref="WritePropertyName" /> call followed by exactly one
/// value.
/// </para>
/// <para>
/// A format rejects, at the point it is written, any value its grammar cannot represent — for example a Boolean or
/// floating-point value written to a Bencode document — unless a converter has already reduced it to a representable
/// kind.
/// </para>
/// </remarks>
public interface ISerializationWriter
{
    /// <summary>
    /// Writes the start of an object.
    /// </summary>
    void WriteStartObject();

    /// <summary>
    /// Writes the end of the current object.
    /// </summary>
    void WriteEndObject();

    /// <summary>
    /// Writes the start of an array.
    /// </summary>
    void WriteStartArray();

    /// <summary>
    /// Writes the end of the current array.
    /// </summary>
    void WriteEndArray();

    /// <summary>
    /// Writes the name of the property whose value follows.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    void WritePropertyName(string name);

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    void WriteString(string value);

    /// <summary>
    /// Writes a binary value.
    /// </summary>
    /// <param name="value">The bytes to write.</param>
    void WriteBytes(ReadOnlySpan<byte> value);

    /// <summary>
    /// Writes a 64-bit signed integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    void WriteInt64(long value);

    /// <summary>
    /// Writes a 64-bit unsigned integer value.
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    void WriteUInt64(ulong value);

    /// <summary>
    /// Writes a double-precision floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    void WriteDouble(double value);

    /// <summary>
    /// Writes a Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    void WriteBoolean(bool value);

    /// <summary>
    /// Writes a null value.
    /// </summary>
    void WriteNull();

    /// <summary>
    /// Writes a date-time with offset.
    /// </summary>
    /// <param name="value">The date-time with offset.</param>
    void WriteDateTimeOffset(DateTimeOffset value);

    /// <summary>
    /// Writes a date-time without offset.
    /// </summary>
    /// <param name="value">The date-time.</param>
    void WriteDateTime(DateTime value);

    /// <summary>
    /// Writes a calendar date.
    /// </summary>
    /// <param name="value">The date.</param>
    void WriteDateOnly(DateOnly value);

    /// <summary>
    /// Writes a time of day.
    /// </summary>
    /// <param name="value">The time of day.</param>
    void WriteTimeOnly(TimeOnly value);
}
