// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlInteger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML integer value, stored as a 64-bit signed integer. Decimal, hexadecimal, octal, and binary literals
/// all parse to the same numeric value.
/// </summary>
/// <example>
///<![CDATA[
/// var port = new TomlInteger(8080);
/// long value = port.Value;                       // 8080
///
/// // 0xFF, 0o17, and 0b1010 literals all yield a TomlInteger.
/// long first = ((TomlInteger)ports[0]).Value;
///]]>
/// </example>
public sealed class TomlInteger
    : TomlValue
    , IEquatable<TomlInteger>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlInteger" /> class.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public TomlInteger(long value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.Integer;

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    /// <returns>The underlying 64-bit signed integer.</returns>
    public long Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlInteger? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlInteger);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);
}
