// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Converts an <see cref="Interval{T}" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <typeparam name="T">The numeric endpoint type of the interval.</typeparam>
/// <remarks>
/// <para>
/// The converter shape is selected by <see cref="NumericsJsonPolicy" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Strict" /> — canonical object form
/// <c>{ "lower": 1, "upper": 5, "lowerInclusive": true, "upperInclusive": true }</c>; the empty interval emits as the
/// single-property object <c>{ "empty": true }</c>. A bounded side requires its endpoint value and inclusion flag; an
/// unbounded side emits the marker <c>"lowerUnbounded": true</c> / <c>"upperUnbounded": true</c> in place of both (for
/// example <c>{ "lower": 1, "upperUnbounded": true, "lowerInclusive": true }</c> for <c>[1, +&#x221E;)</c>, or
/// <c>{ "lowerUnbounded": true, "upperUnbounded": true }</c> for the whole line). Duplicate properties are rejected, and
/// an <c>"empty": true</c> marker rejects any sibling endpoint or unbounded property.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Lenient" /> — same object shape as <see cref="NumericsJsonPolicy.Strict" />, with
/// additional tolerance: <c>"min"</c> and <c>"max"</c> are accepted as aliases for <c>"lower"</c> and <c>"upper"</c>,
/// missing endpoint-inclusion flags default to closed (<see langword="true" />), and a top-level JSON string is
/// accepted and routed through <see cref="Interval{T}.TryParse(string?, IFormatProvider?, out Interval{T})" /> as a
/// fallback.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Compact" /> — string form in ISO 31-11 bracket notation (for example <c>"[1, 5)"</c>)
/// or the empty-set glyph <c>"∅"</c>. Reads delegate to
/// <see cref="Interval{T}.TryParse(string?, IFormatProvider?, out Interval{T})" /> and writes call
/// <see cref="Interval{T}.ToString(string?, IFormatProvider?)" /> with the invariant culture.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class IntervalJsonConverter<T>
    : JsonConverter<Interval<T>>
    where T : INumber<T>
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly NumericsJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalJsonConverter{T}" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape.
    /// </summary>
    public IntervalJsonConverter()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalJsonConverter{T}" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public IntervalJsonConverter(NumericsJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override Interval<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy switch
        {
            NumericsJsonPolicy.Compact => ReadCompact(ref reader),
            NumericsJsonPolicy.Lenient when reader.TokenType == JsonTokenType.String => ReadCompact(ref reader),
            _ => ReadObject(ref reader),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Interval<T> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == NumericsJsonPolicy.Compact)
        {
            writer.WriteStringValue(value.ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        if (value.IsEmpty)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("empty", true);
            writer.WriteEndObject();
            return;
        }

        writer.WriteStartObject();

        // Endpoint values (or the unbounded marker) come first; the inclusivity flags trail them so a fully bounded
        // interval keeps the original lower/upper/lowerInclusive/upperInclusive property order. An unbounded side emits
        // only its marker — an infinite endpoint has no value and no inclusion.
        if (value.LowerUnbounded)
        {
            writer.WriteBoolean("lowerUnbounded", true);
        }
        else
        {
            writer.WritePropertyName("lower");
            WriteEndpointValue(writer, value.Lower);
        }

        if (value.UpperUnbounded)
        {
            writer.WriteBoolean("upperUnbounded", true);
        }
        else
        {
            writer.WritePropertyName("upper");
            WriteEndpointValue(writer, value.Upper);
        }

        if (!value.LowerUnbounded)
            writer.WriteBoolean("lowerInclusive", value.LowerInclusive);

        if (!value.UpperUnbounded)
            writer.WriteBoolean("upperInclusive", value.UpperInclusive);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"[1, 5)"</c> or <c>"∅"</c>) and constructs the interval via
    /// <see cref="Interval{T}.TryParse(string?, IFormatProvider?, out Interval{T})" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized interval.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid <see cref="Interval{T}" /> representation.
    /// </exception>
    private static Interval<T> ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    NumericsJsonResourceStrings.Json_Invalid_ExpectedCompactString_Interval,
                    typeof(T).Name));
        }

        string text = reader.GetString() !;
        return !Interval<T>.TryParse(text, CultureInfo.InvariantCulture, out Interval<T> result)
            ? throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    NumericsJsonResourceStrings.Json_Invalid_CompactIntervalForm,
                    text,
                    typeof(T).Name))
            : result;
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same property set; Lenient additionally
    /// accepts <c>"min"</c>/<c>"max"</c> aliases and defaults missing endpoint-inclusion flags to closed).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized interval.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is not an object, declares a duplicate property, mixes an <c>"empty": true</c> marker with
    /// endpoint properties, is missing a required property, or carries a value that cannot be parsed as
    /// <typeparamref name="T" />.
    /// </exception>
    private Interval<T> ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedObject_Interval);

        T? lower = default;
        T? upper = default;
        bool lowerInclusive = false;
        bool upperInclusive = false;
        bool lowerUnbounded = false;
        bool upperUnbounded = false;
        bool empty = false;
        bool lowerSeen = false;
        bool upperSeen = false;
        bool lowerInclusiveSeen = false;
        bool upperInclusiveSeen = false;
        bool lowerUnboundedSeen = false;
        bool upperUnboundedSeen = false;
        bool emptySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString() !;
            if (!reader.Read())
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_UnexpectedEnd);

            if (IsLowerPropertyName(propertyName))
            {
                if (lowerSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateLower);
                lowerSeen = true;
                lower = ReadEndpointValue(ref reader, NumericsJsonResourceStrings.Json_Invalid_LowerMustBeNumber);
            }
            else if (IsUpperPropertyName(propertyName))
            {
                if (upperSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateUpper);
                upperSeen = true;
                upper = ReadEndpointValue(ref reader, NumericsJsonResourceStrings.Json_Invalid_UpperMustBeNumber);
            }
            else if (string.Equals(propertyName, "lowerInclusive", StringComparison.OrdinalIgnoreCase))
            {
                if (lowerInclusiveSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateLowerInclusive);
                lowerInclusiveSeen = true;
                lowerInclusive = ReadBooleanValue(ref reader, "lowerInclusive");
            }
            else if (string.Equals(propertyName, "upperInclusive", StringComparison.OrdinalIgnoreCase))
            {
                if (upperInclusiveSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateUpperInclusive);
                upperInclusiveSeen = true;
                upperInclusive = ReadBooleanValue(ref reader, "upperInclusive");
            }
            else if (string.Equals(propertyName, "lowerUnbounded", StringComparison.OrdinalIgnoreCase))
            {
                if (lowerUnboundedSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateLowerUnbounded);
                lowerUnboundedSeen = true;
                lowerUnbounded = ReadBooleanValue(ref reader, "lowerUnbounded");
            }
            else if (string.Equals(propertyName, "upperUnbounded", StringComparison.OrdinalIgnoreCase))
            {
                if (upperUnboundedSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateUpperUnbounded);
                upperUnboundedSeen = true;
                upperUnbounded = ReadBooleanValue(ref reader, "upperUnbounded");
            }
            else if (string.Equals(propertyName, "empty", StringComparison.OrdinalIgnoreCase))
            {
                if (emptySeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateEmpty);
                emptySeen = true;
                empty = ReadBooleanValue(ref reader, "empty");
            }
            else
            {
                reader.Skip();
            }
        }

        // An "unbounded": false marker is treated as "this side is not unbounded"; an unbounded side takes precedence
        // over any endpoint value that accompanies it.
        bool lowerIsUnbounded = lowerUnboundedSeen && lowerUnbounded;
        bool upperIsUnbounded = upperUnboundedSeen && upperUnbounded;

        if (emptySeen && empty)
        {
            return lowerSeen || upperSeen || lowerInclusiveSeen || upperInclusiveSeen || lowerIsUnbounded || upperIsUnbounded
                ? throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_EmptyIntervalExtraProperties)
                : Interval<T>.Empty;
        }

        if (!lowerIsUnbounded && !lowerSeen)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_MissingLower);

        if (!upperIsUnbounded && !upperSeen)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_MissingUpper);

        bool resolvedLowerInclusive = false;
        bool resolvedUpperInclusive = false;

        if (!lowerIsUnbounded)
        {
            if (_policy == NumericsJsonPolicy.Lenient)
            {
                resolvedLowerInclusive = !lowerInclusiveSeen || lowerInclusive;
            }
            else
            {
                if (!lowerInclusiveSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_MissingLowerInclusive);
                resolvedLowerInclusive = lowerInclusive;
            }
        }

        if (!upperIsUnbounded)
        {
            if (_policy == NumericsJsonPolicy.Lenient)
            {
                resolvedUpperInclusive = !upperInclusiveSeen || upperInclusive;
            }
            else
            {
                if (!upperInclusiveSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_MissingUpperInclusive);
                resolvedUpperInclusive = upperInclusive;
            }
        }

        if (lowerIsUnbounded && upperIsUnbounded)
            return Interval<T>.All;

        if (lowerIsUnbounded)
            return resolvedUpperInclusive ? Interval<T>.AtMost(upper!) : Interval<T>.LessThan(upper!);

        if (upperIsUnbounded)
            return resolvedLowerInclusive ? Interval<T>.AtLeast(lower!) : Interval<T>.GreaterThan(lower!);

        return new Interval<T>(lower!, upper!, resolvedLowerInclusive, resolvedUpperInclusive);
    }

    /// <summary>
    /// Determines whether <paramref name="propertyName" /> matches the lower-endpoint property name, accepting the
    /// <c>"min"</c> alias when the active policy is <see cref="NumericsJsonPolicy.Lenient" />.
    /// </summary>
    /// <param name="propertyName">The property name read from the JSON object.</param>
    /// <returns><see langword="true" /> when the name matches; otherwise <see langword="false" />.</returns>
    private bool IsLowerPropertyName(string propertyName) =>
        string.Equals(propertyName, "lower", StringComparison.OrdinalIgnoreCase)
        || (_policy == NumericsJsonPolicy.Lenient
            && string.Equals(propertyName, "min", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Determines whether <paramref name="propertyName" /> matches the upper-endpoint property name, accepting the
    /// <c>"max"</c> alias when the active policy is <see cref="NumericsJsonPolicy.Lenient" />.
    /// </summary>
    /// <param name="propertyName">The property name read from the JSON object.</param>
    /// <returns><see langword="true" /> when the name matches; otherwise <see langword="false" />.</returns>
    private bool IsUpperPropertyName(string propertyName) =>
        string.Equals(propertyName, "upper", StringComparison.OrdinalIgnoreCase)
        || (_policy == NumericsJsonPolicy.Lenient
            && string.Equals(propertyName, "max", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Reads a <typeparamref name="T" />-typed endpoint value from either a JSON number or a JSON string token, using
    /// the invariant culture so wide-magnitude or high-precision values round-trip without loss.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="typeMismatchMessage">
    /// The resource message reported when the token is neither a number nor a parseable string.
    /// </param>
    /// <returns>The parsed <typeparamref name="T" /> value.</returns>
    /// <exception cref="JsonException">The token is neither a number nor a parseable numeric string.</exception>
    private static T ReadEndpointValue(ref Utf8JsonReader reader, string typeMismatchMessage)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // JSON number tokens are ASCII (digits, sign, fractional and exponent parts), so a 1:1 byte-to-char
            // decode is safe and allows arbitrary-precision numeric backings to flow through without precision loss.
            string text = reader.HasValueSequence
                ? Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                : Encoding.UTF8.GetString(reader.ValueSpan);

            return T.TryParse(text.AsSpan(), NumberStyles.Any, CultureInfo.InvariantCulture, out T? parsed)
                ? parsed
                : throw new JsonException(typeMismatchMessage);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();
            return stringValue is not null
                && T.TryParse(stringValue.AsSpan(), NumberStyles.Any, CultureInfo.InvariantCulture, out T? parsed)
                ? parsed
                : throw new JsonException(typeMismatchMessage);
        }

        throw new JsonException(typeMismatchMessage);
    }

    /// <summary>
    /// Reads a boolean property value, reporting a contract violation when the token is not a JSON boolean.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="propertyName">The property name being read, embedded in the error message.</param>
    /// <returns>The boolean value.</returns>
    /// <exception cref="JsonException">The token is not <c>true</c> or <c>false</c>.</exception>
    private static bool ReadBooleanValue(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType == JsonTokenType.True)
            return true;

        return reader.TokenType == JsonTokenType.False
            ? false
            : throw new JsonException(
            string.Format(
                CultureInfo.CurrentCulture,
                NumericsJsonResourceStrings.Json_Invalid_PropertyMustBeBoolean,
                propertyName));
    }

    /// <summary>
    /// Writes a <typeparamref name="T" />-typed endpoint value as a raw JSON number using the invariant culture, so
    /// wide-magnitude or high-precision values are not truncated by the writer's primitive overloads.
    /// </summary>
    /// <param name="writer">The writer that receives the value.</param>
    /// <param name="value">The endpoint value to write.</param>
    private static void WriteEndpointValue(Utf8JsonWriter writer, T value)
    {
        string text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
        writer.WriteRawValue(text);
    }
}
