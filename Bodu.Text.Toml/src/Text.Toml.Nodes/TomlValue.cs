// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Represents a scalar TOML value within a node tree — one of the eight TOML scalar kinds: a string, a 64-bit integer,
/// a floating-point number, a Boolean, or one of the four date-time kinds.
/// </summary>
/// <remarks>
/// The stored kind is fixed at construction and reported by <see cref="GetValueKind" />. A scalar reads back as the CLR
/// type that matches its kind; an integer additionally reads back as any fixed-width integer type through a checked
/// conversion, and a float reads back as either <see cref="double" /> or <see cref="float" />.
/// </remarks>
public sealed class TomlValue
    : TomlNode
{
    /// <summary>
    /// The kind of scalar this value holds.
    /// </summary>
    private readonly TomlValueKind _kind;

    /// <summary>
    /// The boxed scalar payload, whose runtime type matches <see cref="_kind" />.
    /// </summary>
    private readonly object _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlValue" /> class holding the supplied boxed scalar.
    /// </summary>
    /// <param name="kind">The scalar kind.</param>
    /// <param name="value">The boxed scalar payload.</param>
    private TomlValue(TomlValueKind kind, object value)
    {
        _kind = kind;
        _value = value;
    }

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new string-valued node.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static TomlValue Create(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        return new TomlValue(TomlValueKind.String, value);
    }

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied 64-bit integer.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new integer-valued node.</returns>
    public static TomlValue Create(long value) =>
        new(TomlValueKind.Integer, value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied 32-bit integer.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new integer-valued node.</returns>
    public static TomlValue Create(int value) =>
        Create((long)value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>A new floating-point node.</returns>
    public static TomlValue Create(double value) =>
        new(TomlValueKind.Float, value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>A new Boolean node.</returns>
    public static TomlValue Create(bool value) =>
        new(TomlValueKind.Boolean, value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied offset date-time value.
    /// </summary>
    /// <param name="value">The offset date-time value.</param>
    /// <returns>A new offset-date-time node.</returns>
    public static TomlValue Create(DateTimeOffset value) =>
        new(TomlValueKind.OffsetDateTime, value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied local date-time value.
    /// </summary>
    /// <param name="value">The local date-time value.</param>
    /// <returns>A new local-date-time node.</returns>
    public static TomlValue Create(DateTime value) =>
        new(TomlValueKind.LocalDateTime, value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied local date value.
    /// </summary>
    /// <param name="value">The local date value.</param>
    /// <returns>A new local-date node.</returns>
    public static TomlValue Create(DateOnly value) =>
        new(TomlValueKind.LocalDate, value);

    /// <summary>
    /// Creates a <see cref="TomlValue" /> holding the supplied local time value.
    /// </summary>
    /// <param name="value">The local time value.</param>
    /// <returns>A new local-time node.</returns>
    public static TomlValue Create(TimeOnly value) =>
        new(TomlValueKind.LocalTime, value);

    /// <inheritdoc />
    public override TomlValueKind GetValueKind() =>
        _kind;

    /// <summary>
    /// Returns the scalar value converted to the requested type.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <returns>The converted scalar value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stored value cannot be converted to <typeparamref name="T" />.
    /// </exception>
    public new T GetValue<T>()
    {
        if (TryGetValue(out T value))
            return value;

        throw new InvalidOperationException(
            string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_NodeValueConversion, typeof(T), _kind));
    }

    /// <summary>
    /// Attempts to convert the scalar value to the requested type.
    /// </summary>
    /// <typeparam name="T">The type to convert the scalar value to.</typeparam>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the converted value; otherwise the default value of
    /// <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the stored value was converted to <typeparamref name="T" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// A string value converts to <see cref="string" />; an integer value converts to any fixed-width integer type
    /// through a checked conversion that fails when the value is out of range; a float value converts to
    /// <see cref="double" /> or <see cref="float" />; a Boolean value converts to <see cref="bool" />; and each
    /// date-time kind converts to its matching CLR type. Any other combination of stored kind and requested type yields
    /// <see langword="false" />.
    /// </remarks>
    public bool TryGetValue<T>(out T value)
    {
        if (_kind == TomlValueKind.Integer)
            return TryGetInteger(out value);

        if (_kind == TomlValueKind.Float && typeof(T) == typeof(float))
        {
            value = (T)(object)(float)(double)_value;
            return true;
        }

        if (_value is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public override void WriteTo(Utf8TomlWriter writer)
    {
        switch (_kind)
        {
            case TomlValueKind.String:
                writer.WriteString((string)_value);
                break;

            case TomlValueKind.Integer:
                writer.WriteInteger((long)_value);
                break;

            case TomlValueKind.Float:
                writer.WriteFloat((double)_value);
                break;

            case TomlValueKind.Boolean:
                writer.WriteBoolean((bool)_value);
                break;

            case TomlValueKind.OffsetDateTime:
                writer.WriteOffsetDateTime((DateTimeOffset)_value);
                break;

            case TomlValueKind.LocalDateTime:
                writer.WriteLocalDateTime((DateTime)_value);
                break;

            case TomlValueKind.LocalDate:
                writer.WriteLocalDate((DateOnly)_value);
                break;

            default:
                writer.WriteLocalTime((TimeOnly)_value);
                break;
        }
    }

    /// <inheritdoc />
    public override TomlNode DeepClone() =>
        new TomlValue(_kind, _value);

    /// <inheritdoc />
    public override string ToString() =>
        _kind switch
        {
            TomlValueKind.String => (string)_value,
            TomlValueKind.Integer => ((long)_value).ToString(CultureInfo.InvariantCulture),
            TomlValueKind.Float => ((double)_value).ToString(CultureInfo.InvariantCulture),
            TomlValueKind.Boolean => (bool)_value ? "true" : "false",
            TomlValueKind.OffsetDateTime => ((DateTimeOffset)_value).ToString("o", CultureInfo.InvariantCulture),
            TomlValueKind.LocalDateTime => ((DateTime)_value).ToString("s", CultureInfo.InvariantCulture),
            TomlValueKind.LocalDate => ((DateOnly)_value).ToString("o", CultureInfo.InvariantCulture),
            _ => ((TimeOnly)_value).ToString("o", CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Determines whether this scalar value is structurally equal to another.
    /// </summary>
    /// <param name="other">The other scalar value.</param>
    /// <returns>
    /// <see langword="true" /> when both values share the same kind and an equal payload; otherwise
    /// <see langword="false" />.
    /// </returns>
    internal bool ValueEquals(TomlValue other) =>
        _kind == other._kind && _value.Equals(other._value);

    /// <summary>
    /// Attempts to convert the stored integer to the requested type through a checked conversion.
    /// </summary>
    /// <typeparam name="T">The requested type.</typeparam>
    /// <param name="value">When this method returns <see langword="true" />, the converted value.</param>
    /// <returns>
    /// <see langword="true" /> when <typeparamref name="T" /> is a supported integer type and the value is in range.
    /// </returns>
    private bool TryGetInteger<T>(out T value)
    {
        long integer = (long)_value;
        try
        {
            object? boxed = Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.SByte => checked((sbyte)integer),
                TypeCode.Byte => checked((byte)integer),
                TypeCode.Int16 => checked((short)integer),
                TypeCode.UInt16 => checked((ushort)integer),
                TypeCode.Int32 => checked((int)integer),
                TypeCode.UInt32 => checked((uint)integer),
                TypeCode.Int64 => integer,
                TypeCode.UInt64 => checked((ulong)integer),
                _ => null,
            };

            if (boxed is null)
            {
                value = default!;
                return false;
            }

            value = (T)boxed;
            return true;
        }
        catch (OverflowException)
        {
            value = default!;
            return false;
        }
    }
}
