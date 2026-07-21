// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Represents a scalar node in a mutable YAML document object model.
/// </summary>
public sealed class YamlValue
    : YamlNode
{
    /// <summary>The boxed scalar value, or <see langword="null" /> for a null scalar.</summary>
    private readonly object? _value;

    /// <summary>The kind of the scalar value.</summary>
    private readonly YamlValueKind _kind;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlValue" /> class.
    /// </summary>
    /// <param name="value">The boxed scalar value.</param>
    /// <param name="kind">The value kind.</param>
    private YamlValue(object? value, YamlValueKind kind)
    {
        _value = value;
        _kind = kind;
    }

    /// <summary>
    /// Gets the kind of the scalar value.
    /// </summary>
    /// <value>The value kind.</value>
    public YamlValueKind ValueKind => _kind;

    /// <summary>
    /// Creates a string scalar.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The scalar node.</returns>
    public static YamlValue Create(string value) => new(value, YamlValueKind.String);

    /// <summary>
    /// Creates an integer scalar.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The scalar node.</returns>
    public static YamlValue Create(long value) => new(value, YamlValueKind.Integer);

    /// <summary>
    /// Creates a floating-point scalar.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>The scalar node.</returns>
    public static YamlValue Create(double value) => new(value, YamlValueKind.Float);

    /// <summary>
    /// Creates a boolean scalar.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>The scalar node.</returns>
    public static YamlValue Create(bool value) => new(value, YamlValueKind.Boolean);

    /// <summary>
    /// Gets the scalar value converted to the requested type.
    /// </summary>
    /// <typeparam name="T">The desired value type.</typeparam>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value cannot be converted to <typeparamref name="T" />.
    /// </exception>
    public T GetValue<T>()
    {
        if (_value is T typed)
            return typed;

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)ToScalarString();

            return (T)Convert.ChangeType(_value!, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlValueConversion, typeof(T)), ex);
        }
    }

    /// <inheritdoc />
    public override void WriteTo(Utf8YamlWriter writer)
    {
        switch (_kind)
        {
            case YamlValueKind.String:
                writer.WriteString((string)_value!);
                break;
            case YamlValueKind.Integer:
                writer.WriteInteger((long)_value!);
                break;
            case YamlValueKind.Float:
                writer.WriteDouble((double)_value!);
                break;
            case YamlValueKind.Boolean:
                writer.WriteBoolean((bool)_value!);
                break;
            default:
                writer.WriteNull();
                break;
        }
    }

    /// <summary>
    /// Produces the canonical string form of the scalar.
    /// </summary>
    /// <returns>The scalar text.</returns>
    private string ToScalarString() => _kind switch
    {
        YamlValueKind.String => (string)_value!,
        YamlValueKind.Integer => ((long)_value!).ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Float => ((double)_value!).ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Boolean => (bool)_value! ? "true" : "false",
        _ => string.Empty,
    };
}
