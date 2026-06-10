// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ISerializationReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides a forward-only cursor over a parsed document, surfacing its structure as a stream of format-neutral value
/// tokens. A <see cref="FormatConverter{T}" /> reads from this interface, so a converter written against it works
/// against any format that supplies an implementation.
/// </summary>
/// <remarks>
/// <para>
/// The cursor models the same contract as <see cref="System.Text.Json.Utf8JsonReader" />: a call to <see cref="Read" />
/// advances to the next token, and <see cref="TokenKind" /> reports the token the cursor is currently positioned on. A
/// converter is invoked with the cursor positioned on the first token of the value it must read, and must leave the
/// cursor positioned on that value's last token.
/// </para>
/// <para>
/// The typed accessors (<see cref="GetString" />, <see cref="GetInt64" />, and so on) are valid only when
/// <see cref="TokenKind" /> reports the corresponding kind; calling one for a different kind throws.
/// </para>
/// </remarks>
public interface ISerializationReader
{
    /// <summary>
    /// Gets the kind of the token the cursor is currently positioned on.
    /// </summary>
    /// <returns>
    /// The current token kind, or <see cref="SerializationValueKind.None" /> before the first or after the last token.
    /// </returns>
    SerializationValueKind TokenKind { get; }

    /// <summary>
    /// Gets the source location of the current token.
    /// </summary>
    /// <returns>The location of the current token.</returns>
    TextLocation Location { get; }

    /// <summary>
    /// Advances the cursor to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token is available; <see langword="false" /> at the end of the document.
    /// </returns>
    bool Read();

    /// <summary>
    /// Skips the current value, including the entire subtree when the cursor is on an object or array start.
    /// </summary>
    /// <remarks>
    /// On return the cursor is positioned on the last token of the skipped value (the matching end token for a
    /// container, or the scalar token itself).
    /// </remarks>
    void Skip();

    /// <summary>
    /// Reads the current token as a property name.
    /// </summary>
    /// <returns>The property name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a property name.</exception>
    string GetPropertyName();

    /// <summary>
    /// Reads the current token as a string value.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a string.</exception>
    string GetString();

    /// <summary>
    /// Reads the current token as a binary value.
    /// </summary>
    /// <returns>The byte sequence.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a binary value.</exception>
    ReadOnlyMemory<byte> GetBytes();

    /// <summary>
    /// Reads the current token as a 64-bit signed integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    long GetInt64();

    /// <summary>
    /// Reads the current token as a 64-bit unsigned integer.
    /// </summary>
    /// <returns>The unsigned integer value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token cannot be read as an unsigned integer.
    /// </exception>
    ulong GetUInt64();

    /// <summary>
    /// Reads the current token as a double-precision floating-point value.
    /// </summary>
    /// <returns>The floating-point value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a floating-point value.
    /// </exception>
    double GetDouble();

    /// <summary>
    /// Reads the current token as a Boolean value.
    /// </summary>
    /// <returns>The Boolean value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a Boolean.</exception>
    bool GetBoolean();

    /// <summary>
    /// Reads the current token as a date-time with offset.
    /// </summary>
    /// <returns>The date-time with offset.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a date-time with offset.
    /// </exception>
    DateTimeOffset GetDateTimeOffset();

    /// <summary>
    /// Reads the current token as a date-time without offset.
    /// </summary>
    /// <returns>The date-time.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a local date-time.</exception>
    DateTime GetDateTime();

    /// <summary>
    /// Reads the current token as a calendar date.
    /// </summary>
    /// <returns>The date.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a local date.</exception>
    DateOnly GetDateOnly();

    /// <summary>
    /// Reads the current token as a time of day.
    /// </summary>
    /// <returns>The time of day.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not a local time.</exception>
    TimeOnly GetTimeOnly();
}
