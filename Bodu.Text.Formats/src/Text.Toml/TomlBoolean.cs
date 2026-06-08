// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlBoolean.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML boolean value.
/// </summary>
public sealed class TomlBoolean : TomlValue, IEquatable<TomlBoolean>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlBoolean" /> class.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public TomlBoolean(bool value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.Boolean;

    /// <summary>
    /// Gets a value indicating whether the underlying boolean is <see langword="true" />.
    /// </summary>
    /// <returns>The underlying boolean.</returns>
    public bool Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlBoolean? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlBoolean);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value ? "true" : "false";
}
