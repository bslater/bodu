// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Serialization.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T> :
    IXmlSerializable
{
    /// <inheritdoc />
    XmlSchema? IXmlSerializable.GetSchema() =>
        null;

    /// <inheritdoc />
    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        ThrowHelper.ThrowIfNull(reader);

        string content = reader.ReadElementContentAsString();

        // The struct is immutable, so deserialization rebuilds the value and writes it back through a mutable
        // reference to the boxed instance the serializer passes as the receiver.
        Unsafe.AsRef(in this) = Parse(content, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.WriteString(ToString(null, CultureInfo.InvariantCulture));
    }
}
