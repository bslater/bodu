// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Serializes this rational value to JSON using the type's default policy.
    /// </summary>
    /// <returns>The JSON text representing this value.</returns>
    /// <remarks>
    /// <para>
    /// The default attribute-driven policy is <c>Strict</c>, which emits the canonical object form
    /// <c>{ "numerator": …, "denominator": … }</c>. Callers needing the compact <c>"3/4"</c> string form should
    /// serialize through a <see cref="JsonSerializerOptions" /> on which
    /// <c>AddNumericsJsonConverters(NumericsJsonPolicy.Compact)</c> has been called.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization of Fraction<T> uses the reflection-based JsonSerializer. Use AddNumericsJsonConverters with a source-generated JsonSerializerContext for trimming and AOT.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization of Fraction<T> uses the reflection-based JsonSerializer. Use AddNumericsJsonConverters with a source-generated JsonSerializerContext for trimming and AOT.")]
    public string ToJson() =>
        JsonSerializer.Serialize(this);

    /// <summary>
    /// Deserializes a <see cref="Fraction{T}" /> from its JSON representation using the type's default policy.
    /// </summary>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <returns>The rational value the JSON text represents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="json" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="JsonException">Thrown if <paramref name="json" /> is not a valid fraction.</exception>
    /// <remarks>
    /// <para>
    /// The default attribute-driven policy is <c>Strict</c>, which expects the canonical object form. Callers needing
    /// to accept the compact <c>"3/4"</c> string form should deserialize through a <see cref="JsonSerializerOptions" />
    /// on which <c>AddNumericsJsonConverters(NumericsJsonPolicy.Compact)</c> has been called.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON deserialization of Fraction<T> uses the reflection-based JsonSerializer. Use AddNumericsJsonConverters with a source-generated JsonSerializerContext for trimming and AOT.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON deserialization of Fraction<T> uses the reflection-based JsonSerializer. Use AddNumericsJsonConverters with a source-generated JsonSerializerContext for trimming and AOT.")]
    public static Fraction<T> FromJson(string json)
    {
        ThrowHelper.ThrowIfNull(json);

        return JsonSerializer.Deserialize<Fraction<T>>(json);
    }

    /// <summary>
    /// Serializes this rational value to a self-contained XML element.
    /// </summary>
    /// <returns>The XML text representing this value.</returns>
    public string ToXml() =>
        new XElement("fraction", ToString(null, CultureInfo.InvariantCulture)).ToString();

    /// <summary>
    /// Deserializes a <see cref="Fraction{T}" /> from the XML element produced by <see cref="ToXml" />.
    /// </summary>
    /// <param name="xml">The XML text to deserialize.</param>
    /// <returns>The rational value the XML text represents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="xml" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Xml.XmlException">Thrown if <paramref name="xml" /> is not well-formed XML.</exception>
    /// <exception cref="FormatException">Thrown if the element content is not a valid fraction.</exception>
    public static Fraction<T> FromXml(string xml)
    {
        ThrowHelper.ThrowIfNull(xml);

        return Parse(XElement.Parse(xml).Value, CultureInfo.InvariantCulture);
    }
}
