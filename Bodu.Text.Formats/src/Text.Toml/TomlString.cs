// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML string value.
/// </summary>
public sealed class TomlString : TomlValue, IEquatable<TomlString>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlString" /> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public TomlString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.String;

    /// <summary>
    /// Gets the string value.
    /// </summary>
    /// <returns>The underlying string.</returns>
    public string Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlString? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlString);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() =>
        Value;
}
