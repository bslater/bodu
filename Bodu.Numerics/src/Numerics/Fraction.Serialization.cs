// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Serialization.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Serializes this rational value to its JSON string representation.
    /// </summary>
    /// <returns>The JSON text representing this value.</returns>
    public string ToJson() =>
        JsonSerializer.Serialize(this);

    /// <summary>
    /// Deserializes a <see cref="Fraction{T}" /> from its JSON string representation.
    /// </summary>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <returns>The rational value the JSON text represents.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="json" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="JsonException">Thrown if <paramref name="json" /> is not a valid fraction.</exception>
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
