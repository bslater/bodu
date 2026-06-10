// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLocalDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents a TOML local date — a calendar date without any offset or time-zone relation.
/// </summary>
/// <example>
///<![CDATA[
/// var date = new TomlLocalDate(new DateOnly(1979, 5, 27));
/// DateOnly value = date.Value;            / 1979-05-27
///]]>
/// </example>
public sealed class TomlLocalDate
    : TomlValue
    , IEquatable<TomlLocalDate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlLocalDate" /> class.
    /// </summary>
    /// <param name="value">The calendar date.</param>
    public TomlLocalDate(DateOnly value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override TomlValueKind Kind => TomlValueKind.LocalDate;

    /// <summary>
    /// Gets the local date value.
    /// </summary>
    /// <returns>The underlying <see cref="DateOnly" />.</returns>
    public DateOnly Value { get; }

    /// <inheritdoc />
    public bool Equals(TomlLocalDate? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as TomlLocalDate);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
