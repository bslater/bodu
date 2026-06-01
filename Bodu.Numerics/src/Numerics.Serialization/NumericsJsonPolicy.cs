// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericsJsonPolicy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics.Serialization;

/// <summary>
/// Selects the JSON serialization shape and parsing strictness applied by the <see cref="Bodu.Numerics" /> JSON
/// converters.
/// </summary>
/// <remarks>
/// <para>
/// The policy is supplied either by constructing a converter directly (e.g.
/// <c>new FractionJsonConverter&lt;int&gt;(NumericsJsonPolicy.Compact)</c>) or, more commonly, through
/// <see cref="NumericsJsonSerializerOptionsExtensions.AddNumericsJsonConverters" /> which registers a coherent set of
/// converters on a <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </para>
/// <para>
/// The shipped <c>[JsonConverter]</c> attributes on <see cref="Fraction{T}" /> and <see cref="Interval{T}" /> default
/// to <see cref="Strict" />. Consumers who need a different shape register their own policy through
/// <see cref="NumericsJsonSerializerOptionsExtensions.AddNumericsJsonConverters" />; converters registered on
/// <see cref="System.Text.Json.JsonSerializerOptions.Converters" /> take precedence over the type-level attribute.
/// </para>
/// </remarks>
public enum NumericsJsonPolicy
{
    /// <summary>
    /// Canonical JSON shape suitable for persistence and interchange data: <see cref="Fraction{T}" /> serializes as an
    /// object with <c>"numerator"</c> and <c>"denominator"</c> properties; <see cref="Interval{T}" /> serializes as an
    /// object with <c>"lower"</c>, <c>"upper"</c>, <c>"lowerInclusive"</c>, and <c>"upperInclusive"</c> properties (or
    /// the single-property object <c>{ "empty": true }</c> for an empty interval). Property names compare
    /// case-insensitively, duplicate properties are rejected, and unknown properties are ignored.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Same on-the-wire shape as <see cref="Strict" />, with additional tolerance for import workflows: a top-level
    /// JSON string is accepted and routed through the compact parser as a fallback; for <see cref="Interval{T}" /> the
    /// <c>"min"</c> and <c>"max"</c> aliases are accepted for <c>"lower"</c> and <c>"upper"</c>, and missing endpoint
    /// inclusion properties default to closed. Intended for spreadsheet and external-feed ingest. Not suitable as a
    /// canonical storage shape.
    /// </summary>
    Lenient = 1,

    /// <summary>
    /// Compact JSON shape: <see cref="Fraction{T}" /> serializes as a single JSON string of the form
    /// <c>"&lt;numerator&gt;/&lt;denominator&gt;"</c> (for example <c>"3/4"</c>); <see cref="Interval{T}" /> serializes
    /// as a JSON string in ISO 31-11 bracket notation (for example <c>"[1, 5)"</c>) or as the empty-set glyph
    /// <c>"∅"</c>. Reads delegate to the type's <c>Parse</c> / <c>TryParse</c> path.
    /// </summary>
    Compact = 2,
}
