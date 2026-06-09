// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlOffsetDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML offset date-time — an RFC 3339 instant carrying a time offset. Unlike the three local date-time
/// kinds, it pins the value to an unambiguous point in time.
/// </summary>
/// <example>
///<![CDATA[
/// var dob = new TomlOffsetDateTime(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));
/// DateTimeOffset value = dob.Value;              // 1979-05-27T07:32:00+00:00
///]]>
/// </example>
public sealed class TomlOffsetDateTime
    : TomlValue
    , IEquatable<TomlOffsetDateTime>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlOffsetDateTime" /> class.
    /// </summary>
    /// <param name="value">The instant with its offset.</param>
    public TomlOffsetDateTime(DateTimeOffset value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.OffsetDateTime;

    /// <summary>
    /// Gets the offset date-time value.
    /// </summary>
    /// <returns>The underlying <see cref="DateTimeOffset" />.</returns>
    public DateTimeOffset Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlOffsetDateTime? other) =>
        other is not null && Value.EqualsExact(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlOffsetDateTime);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString("O");
}
