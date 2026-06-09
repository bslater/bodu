// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFloat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML float value, stored as an IEEE 754 binary64 floating-point number.
/// </summary>
public sealed class TomlFloat
    : TomlValue
    , IEquatable<TomlFloat>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlFloat" /> class.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    public TomlFloat(double value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.Float;

    /// <summary>
    /// Gets the floating-point value.
    /// </summary>
    /// <returns>The underlying double-precision value.</returns>
    public double Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlFloat? other) =>
        other is not null && Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlFloat);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString("R", CultureInfo.InvariantCulture);
}
