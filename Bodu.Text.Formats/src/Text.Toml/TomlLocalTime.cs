// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLocalTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML local time — a time of day without any offset or time-zone relation.
/// </summary>
public sealed class TomlLocalTime
    : TomlValue
    , IEquatable<TomlLocalTime>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlLocalTime" /> class.
    /// </summary>
    /// <param name="value">The time of day.</param>
    public TomlLocalTime(TimeOnly value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.LocalTime;

    /// <summary>
    /// Gets the local time value.
    /// </summary>
    /// <returns>The underlying <see cref="TimeOnly" />.</returns>
    public TimeOnly Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlLocalTime? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlLocalTime);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString("HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture);
}
