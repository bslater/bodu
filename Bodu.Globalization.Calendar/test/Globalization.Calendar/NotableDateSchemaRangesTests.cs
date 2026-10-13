// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRangesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bounds <see cref="NotableDateSchemaRanges" /> holds for the JSON reader, which must be those the
/// notable-date XML schema's integer types allow, as the schema's own datatypes accept them.
/// </summary>
[TestClass]
public sealed partial class NotableDateSchemaRangesTests
{
    /// <summary>The XML namespace of the notable-date document vocabulary.</summary>
    private const string DocumentNamespace = "urn:bodu:globalization:calendar";

    /// <summary>The XSD type each range stands for: a built-in type of the XML Schema vocabulary, or one the notable-date XSD declares.</summary>
    private static readonly Dictionary<NotableDateSchemaRange, XmlQualifiedName> s_schemaTypes = new()
    {
        [NotableDateSchemaRange.PositiveInteger] = new("positiveInteger", XmlSchema.Namespace),
        [NotableDateSchemaRange.UnsignedShort] = new("unsignedShort", XmlSchema.Namespace),
        [NotableDateSchemaRange.DayValue] = new("dayValue", DocumentNamespace),
        [NotableDateSchemaRange.NonZeroOrdinal] = new("nonZeroOrdinal", DocumentNamespace),
        [NotableDateSchemaRange.IsoWeekValue] = new("isoWeekValue", DocumentNamespace),
    };

    /// <summary>
    /// Returns the XSD datatype a range stands for, compiled from the embedded notable-date XSD.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The datatype.</returns>
    private static XmlSchemaDatatype SchemaDatatype(NotableDateSchemaRange range)
    {
        XmlQualifiedName name = s_schemaTypes[range];
        if (name.Namespace == XmlSchema.Namespace)
            return XmlSchemaType.GetBuiltInSimpleType(name)!.Datatype!;

        using Stream? stream = typeof(NotableDateSchemaRanges).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.xsd");
        Assert.IsNotNull(stream);

        XmlSchemaSet schemas = new();
        using (XmlReader reader = XmlReader.Create(stream))
            _ = schemas.Add(DocumentNamespace, reader);

        schemas.Compile();
        return ((XmlSchemaSimpleType)schemas.GlobalTypes[name]!).Datatype!;
    }

    /// <summary>
    /// Determines whether an XSD datatype accepts a value, its facets included.
    /// </summary>
    /// <param name="datatype">The datatype.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the datatype accepts the value; otherwise <see langword="false" />.</returns>
    private static bool Accepts(XmlSchemaDatatype datatype, long value)
    {
        try
        {
            _ = datatype.ParseValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture), new NameTable(), null);
            return true;
        }
        catch (XmlSchemaException)
        {
            return false;
        }
    }
}
