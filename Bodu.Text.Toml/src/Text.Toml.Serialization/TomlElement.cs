// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Represents a captured TOML value subtree — a scalar, an array, or a table — preserved verbatim so it can be read
/// from a document and written back out unchanged. It is the value type of an extension-data member, filling the role
/// that <see cref="System.Text.Json.Nodes.JsonNode" /> fills for <c>System.Text.Json</c> when capturing unmapped
/// members.
/// </summary>
/// <remarks>
/// <para>
/// An element wraps one of three shapes: a scalar carrying a CLR value and the <see cref="TomlTokenType" /> that
/// produced it; an ordered array of child elements; or a string-keyed table of child elements. The shape is fixed at
/// construction and inspected through <see cref="ValueKind" />.
/// </para>
/// <para>
/// The serializer materializes an element by reading the current value from a <see cref="Utf8TomlReader" /> with
/// <see cref="ReadFrom(ref Utf8TomlReader)" />, and re-emits one through <see cref="WriteTo(Utf8TomlWriter)" />, so a
/// member annotated with <see cref="TomlExtensionDataAttribute" /> round-trips unmatched keys losslessly.
/// </para>
/// </remarks>
public sealed class TomlElement
{
    /// <summary>
    /// The boxed scalar value when <see cref="ValueKind" /> is a scalar kind; otherwise <see langword="null" />.
    /// </summary>
    private readonly object? _scalar;

    /// <summary>
    /// The ordered child elements when <see cref="ValueKind" /> is <see cref="TomlValueKind.Array" />; otherwise
    /// <see langword="null" />.
    /// </summary>
    private readonly List<TomlElement?>? _array;

    /// <summary>
    /// The string-keyed child elements when <see cref="ValueKind" /> is <see cref="TomlValueKind.Table" />; otherwise
    /// <see langword="null" />.
    /// </summary>
    private readonly Dictionary<string, TomlElement?>? _table;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlElement" /> class wrapping a scalar value.
    /// </summary>
    /// <param name="valueKind">The scalar value kind.</param>
    /// <param name="scalar">The boxed scalar value.</param>
    private TomlElement(TomlValueKind valueKind, object? scalar)
    {
        ValueKind = valueKind;
        _scalar = scalar;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlElement" /> class wrapping an array.
    /// </summary>
    /// <param name="array">The child elements.</param>
    private TomlElement(List<TomlElement?> array)
    {
        ValueKind = TomlValueKind.Array;
        _array = array;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlElement" /> class wrapping a table.
    /// </summary>
    /// <param name="table">The string-keyed child elements.</param>
    private TomlElement(Dictionary<string, TomlElement?> table)
    {
        ValueKind = TomlValueKind.Table;
        _table = table;
    }

    /// <summary>
    /// Gets the kind of TOML value this element represents.
    /// </summary>
    /// <returns>The value kind.</returns>
    public TomlValueKind ValueKind { get; }

    /// <summary>
    /// Creates an element wrapping the specified 64-bit integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The new integer element.</returns>
    public static TomlElement Create(long value) =>
        new(TomlValueKind.Integer, value);

    /// <summary>
    /// Creates an element wrapping the specified 32-bit integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The new integer element.</returns>
    public static TomlElement Create(int value) =>
        new(TomlValueKind.Integer, (long)value);

    /// <summary>
    /// Creates an element wrapping the specified string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The new string element.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static TomlElement Create(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        return new TomlElement(TomlValueKind.String, value);
    }

    /// <summary>
    /// Creates an element wrapping the specified floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>The new floating-point element.</returns>
    public static TomlElement Create(double value) =>
        new(TomlValueKind.Float, value);

    /// <summary>
    /// Creates an element wrapping the specified Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>The new Boolean element.</returns>
    public static TomlElement Create(bool value) =>
        new(TomlValueKind.Boolean, value);

    /// <summary>
    /// Reads the value the reader is positioned on, materializing it — together with any nested array or table — into a
    /// new <see cref="TomlElement" />.
    /// </summary>
    /// <param name="reader">The reader positioned on the first token of the value.</param>
    /// <returns>The captured element.</returns>
    /// <exception cref="TomlSerializationException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    /// <remarks>
    /// On entry the reader is positioned on the value's first token. On return it is positioned on the value's last
    /// token, mirroring the contract of <see cref="TomlConverter{T}.Read" />.
    /// </remarks>
    public static TomlElement ReadFrom(ref Utf8TomlReader reader)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.String:
                return new TomlElement(TomlValueKind.String, reader.GetString());
            case TomlTokenType.Integer:
                return new TomlElement(TomlValueKind.Integer, reader.GetInt64());
            case TomlTokenType.Float:
                return new TomlElement(TomlValueKind.Float, reader.GetDouble());
            case TomlTokenType.Boolean:
                return new TomlElement(TomlValueKind.Boolean, reader.GetBoolean());
            case TomlTokenType.OffsetDateTime:
                return new TomlElement(TomlValueKind.OffsetDateTime, reader.GetDateTimeOffset());
            case TomlTokenType.LocalDateTime:
                return new TomlElement(TomlValueKind.LocalDateTime, reader.GetDateTime());
            case TomlTokenType.LocalDate:
                return new TomlElement(TomlValueKind.LocalDate, reader.GetDateOnly());
            case TomlTokenType.LocalTime:
                return new TomlElement(TomlValueKind.LocalTime, reader.GetTimeOnly());

            case TomlTokenType.StartArray:
                List<TomlElement?> items = [];
                while (reader.Read() && reader.TokenType != TomlTokenType.EndArray)
                    items.Add(ReadFrom(ref reader));

                return new TomlElement(items);

            case TomlTokenType.StartTable:
                Dictionary<string, TomlElement?> entries = new(StringComparer.Ordinal);
                while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
                {
                    var key = reader.GetString();
                    reader.Read();
                    entries[key] = ReadFrom(ref reader);
                }

                return new TomlElement(entries);

            default:
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedValue, reader.TokenType));
        }
    }

    /// <summary>
    /// Returns the scalar value of this element converted to <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the element does not wrap a scalar value.</exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the scalar value cannot be converted to <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>()
    {
        if (_scalar is null)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ElementNotScalar, ValueKind));
        }

        if (_scalar is T typed)
            return typed;

        return (T)Convert.ChangeType(_scalar, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Writes this element to the specified writer, emitting any nested array or table in full.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <remarks>
    /// The element is written as a value, so the caller must already have written the property name when the element is
    /// a table or array member.
    /// </remarks>
    public void WriteTo(Utf8TomlWriter writer)
    {
        switch (ValueKind)
        {
            case TomlValueKind.String:
                writer.WriteString((string)_scalar!);
                break;
            case TomlValueKind.Integer:
                writer.WriteInteger((long)_scalar!);
                break;
            case TomlValueKind.Float:
                writer.WriteFloat((double)_scalar!);
                break;
            case TomlValueKind.Boolean:
                writer.WriteBoolean((bool)_scalar!);
                break;
            case TomlValueKind.OffsetDateTime:
                writer.WriteOffsetDateTime((DateTimeOffset)_scalar!);
                break;
            case TomlValueKind.LocalDateTime:
                writer.WriteLocalDateTime((DateTime)_scalar!);
                break;
            case TomlValueKind.LocalDate:
                writer.WriteLocalDate((DateOnly)_scalar!);
                break;
            case TomlValueKind.LocalTime:
                writer.WriteLocalTime((TimeOnly)_scalar!);
                break;

            case TomlValueKind.Array:
                writer.WriteStartArray();
                foreach (TomlElement? item in _array!)
                    (item ?? throw NullChildError()).WriteTo(writer);
                writer.WriteEndArray();
                break;

            default:
                writer.WriteStartTable();
                foreach (KeyValuePair<string, TomlElement?> entry in _table!)
                {
                    if (entry.Value is null)
                        continue;

                    writer.WritePropertyName(entry.Key);
                    entry.Value.WriteTo(writer);
                }

                writer.WriteEndTable();
                break;
        }
    }

    /// <summary>
    /// Creates the exception thrown when an array element captured for re-emission is <see langword="null" />, which
    /// TOML cannot represent.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static TomlSerializationException NullChildError() =>
        new(TomlResourceStrings.Op_NotSupported_NullCollectionElement);
}
