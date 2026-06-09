// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLocalDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML local date-time — a date and time without any offset or time-zone relation. The wrapped
/// <see cref="DateTime" /> always has <see cref="DateTimeKind.Unspecified" /> so it carries no spurious time-zone
/// relation.
/// </summary>
/// <example>
///<![CDATA[
/// var stamp = new TomlLocalDateTime(new DateTime(1979, 5, 27, 7, 32, 0));
/// DateTime value = stamp.Value;                  // Kind == Unspecified
///]]>
/// </example>
public sealed class TomlLocalDateTime
    : TomlValue
    , IEquatable<TomlLocalDateTime>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlLocalDateTime" /> class.
    /// </summary>
    /// <param name="value">The local date and time. Its <see cref="DateTime.Kind" /> is ignored.</param>
    public TomlLocalDateTime(DateTime value)
    {
        Value = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.LocalDateTime;

    /// <summary>
    /// Gets the local date-time value.
    /// </summary>
    /// <returns>The underlying <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" />.</returns>
    public DateTime Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlLocalDateTime? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlLocalDateTime);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture);
}
