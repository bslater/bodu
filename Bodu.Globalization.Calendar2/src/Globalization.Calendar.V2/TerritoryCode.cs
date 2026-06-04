// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents a validated territory code composed of an ISO 3166-1 alpha-2 country and an optional subdivision (for
/// example <c>AU</c> or <c>AU-NSW</c>), with parent/child containment semantics matching the resolution engine.
/// </summary>
/// <remarks>
/// <para>
/// The engine accepts territories as plain strings; this value type is an opt-in convenience that parses, normalizes,
/// and decomposes a code, and interoperates with the string surface through an implicit conversion to its canonical
/// form. A country-only code is a parent of every subdivision sharing its country: <c>AU</c> contains <c>AU-NSW</c>,
/// mirroring how a subnational query matches a rule scoped to its parent territory.
/// </para>
/// <para>
/// The default value is empty (<see cref="IsEmpty" /> is <see langword="true" />); it parses no code, contains nothing,
/// and renders as the empty string.
/// </para>
/// </remarks>
public readonly struct TerritoryCode : IEquatable<TerritoryCode>
{
    /// <summary>
    /// The normalized uppercase country part, or <see langword="null" /> for the default (empty) value.
    /// </summary>
    private readonly string? _country;

    /// <summary>
    /// The normalized uppercase subdivision part, or <see langword="null" /> when the code is country-only.
    /// </summary>
    private readonly string? _subdivision;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerritoryCode" /> struct from already-validated, normalized parts.
    /// </summary>
    /// <param name="country">The normalized uppercase country part.</param>
    /// <param name="subdivision">The normalized uppercase subdivision part, or <see langword="null" />.</param>
    private TerritoryCode(string country, string? subdivision)
    {
        this._country = country;
        this._subdivision = subdivision;
    }

    /// <summary>
    /// Gets a value indicating whether this is the default (empty) code.
    /// </summary>
    /// <returns><see langword="true" /> when no code is held; otherwise <see langword="false" />.</returns>
    public bool IsEmpty =>
        this._country is null;

    /// <summary>
    /// Gets the country part of the code.
    /// </summary>
    /// <returns>The uppercase two-letter country, or the empty string for the default value.</returns>
    public string Country =>
        this._country ?? string.Empty;

    /// <summary>
    /// Gets the subdivision part of the code.
    /// </summary>
    /// <returns>The uppercase subdivision, or <see langword="null" /> when the code is country-only.</returns>
    public string? Subdivision =>
        this._subdivision;

    /// <summary>
    /// Gets a value indicating whether the code names a subdivision rather than a bare country.
    /// </summary>
    /// <returns><see langword="true" /> when a subdivision is present; otherwise <see langword="false" />.</returns>
    public bool IsSubdivision =>
        this._subdivision is not null;

    /// <summary>
    /// Gets the parent country code for a subdivision.
    /// </summary>
    /// <returns>
    /// The country-only <see cref="TerritoryCode" /> for a subdivision, the same code when it is already country-only,
    /// or the default value when this code is empty.
    /// </returns>
    public TerritoryCode Parent =>
        this._country is null ? default : new TerritoryCode(this._country, null);

    /// <summary>
    /// Converts a string to a <see cref="TerritoryCode" />, throwing when the string is not a valid code.
    /// </summary>
    /// <param name="value">The code to convert.</param>
    /// <returns>The parsed code.</returns>
    public static explicit operator TerritoryCode(string value) =>
        Parse(value);

    /// <summary>
    /// Converts a <see cref="TerritoryCode" /> to its canonical string form.
    /// </summary>
    /// <param name="code">The code to convert.</param>
    /// <returns>The canonical code string, accepted directly by the string-based resolution surface.</returns>
    public static implicit operator string(TerritoryCode code) =>
        code.ToString();

    /// <summary>
    /// Determines whether two codes are equal.
    /// </summary>
    /// <param name="left">The first code.</param>
    /// <param name="right">The second code.</param>
    /// <returns><see langword="true" /> when the codes are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(TerritoryCode left, TerritoryCode right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two codes are not equal.
    /// </summary>
    /// <param name="left">The first code.</param>
    /// <param name="right">The second code.</param>
    /// <returns><see langword="true" /> when the codes differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(TerritoryCode left, TerritoryCode right) =>
        !left.Equals(right);

    /// <summary>
    /// Parses a territory code, throwing when it is malformed.
    /// </summary>
    /// <param name="value">The code to parse.</param>
    /// <returns>The parsed code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="value" /> is not a two-letter country optionally followed by a hyphen and a one-to-three
    /// alphanumeric subdivision.
    /// </exception>
    public static TerritoryCode Parse(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (!TryParse(value, out TerritoryCode result))
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Format_Invalid_TerritoryCode, value));

        return result;
    }

    /// <summary>
    /// Attempts to parse a territory code without throwing.
    /// </summary>
    /// <param name="value">The code to parse, which may be <see langword="null" />.</param>
    /// <param name="result">When this method returns, the parsed code, or the default value on failure.</param>
    /// <returns><see langword="true" /> when parsing succeeds; otherwise <see langword="false" />.</returns>
    public static bool TryParse(string? value, out TerritoryCode result)
    {
        result = default;
        if (string.IsNullOrEmpty(value))
            return false;

        int dash = value.IndexOf('-', StringComparison.Ordinal);
        string country = dash < 0 ? value : value.Substring(0, dash);
        string? subdivision = dash < 0 ? null : value.Substring(dash + 1);

        if (!IsValidCountry(country) || (subdivision is not null && !IsValidSubdivision(subdivision)))
            return false;

        result = new TerritoryCode(country.ToUpperInvariant(), subdivision?.ToUpperInvariant());
        return true;
    }

    /// <summary>
    /// Parses a comma-separated list of territory codes.
    /// </summary>
    /// <param name="value">The comma-separated codes, which may be <see langword="null" />.</param>
    /// <returns>
    /// The parsed codes in source order, or an empty list when <paramref name="value" /> is <see langword="null" />,
    /// empty, or white space. Blank entries between commas are ignored.
    /// </returns>
    /// <exception cref="FormatException">Any non-blank entry is not a valid territory code.</exception>
    /// <remarks>
    /// This parser is strict: every non-blank entry must be a valid code, otherwise the call throws. Use
    /// <see cref="TryParse" /> per entry when leniency is required.
    /// </remarks>
    public static IReadOnlyList<TerritoryCode> ParseList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        string[] parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        TerritoryCode[] result = new TerritoryCode[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            result[i] = Parse(parts[i]);

        return result;
    }

    /// <summary>
    /// Determines whether this code is, or is the parent of, the supplied code.
    /// </summary>
    /// <param name="other">The candidate code.</param>
    /// <returns>
    /// <see langword="true" /> when the codes are equal, or this code is a bare country and <paramref name="other" />
    /// is a subdivision of it; otherwise <see langword="false" />.
    /// </returns>
    public bool Contains(TerritoryCode other)
    {
        if (this.IsEmpty || other.IsEmpty)
            return false;

        if (this.Equals(other))
            return true;

        return !this.IsSubdivision && string.Equals(this._country, other._country, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool Equals(TerritoryCode other) =>
        string.Equals(this._country, other._country, StringComparison.Ordinal)
        && string.Equals(this._subdivision, other._subdivision, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is TerritoryCode other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(this._country, this._subdivision);

    /// <summary>
    /// Returns the canonical string form of the code.
    /// </summary>
    /// <returns>The country, the country and subdivision joined by a hyphen, or the empty string when default.</returns>
    public override string ToString() =>
        this._country is null ? string.Empty
        : this._subdivision is null ? this._country
        : string.Concat(this._country, "-", this._subdivision);

    /// <summary>
    /// Determines whether a string is a valid two-letter country part.
    /// </summary>
    /// <param name="value">The candidate country.</param>
    /// <returns><see langword="true" /> when valid; otherwise <see langword="false" />.</returns>
    private static bool IsValidCountry(string value) =>
        value.Length == 2 && char.IsAsciiLetter(value[0]) && char.IsAsciiLetter(value[1]);

    /// <summary>
    /// Determines whether a string is a valid one-to-three alphanumeric subdivision part.
    /// </summary>
    /// <param name="value">The candidate subdivision.</param>
    /// <returns><see langword="true" /> when valid; otherwise <see langword="false" />.</returns>
    private static bool IsValidSubdivision(string value)
    {
        if (value.Length is < 1 or > 3)
            return false;

        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c))
                return false;
        }

        return true;
    }
}
