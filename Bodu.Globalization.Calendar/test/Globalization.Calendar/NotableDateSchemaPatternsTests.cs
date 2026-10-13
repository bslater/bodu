// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaPatternsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the patterns <see cref="NotableDateSchemaPatterns" /> holds for the JSON reader, which must be those the
/// notable-date XML schema enforces, matched as the schema matches them.
/// </summary>
[TestClass]
public sealed partial class NotableDateSchemaPatternsTests
{
    /// <summary>The XML namespace of the XML Schema vocabulary.</summary>
    private static readonly XNamespace s_xs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Returns the pattern the embedded notable-date XSD declares for one of its simple types.
    /// </summary>
    /// <param name="simpleType">The name of the simple type.</param>
    /// <returns>The pattern's text.</returns>
    private static string SchemaPattern(string simpleType)
    {
        using Stream? stream = typeof(NotableDateSchemaPatterns).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.xsd");
        Assert.IsNotNull(stream);

        XDocument schema = XDocument.Load(stream);
        XElement type = schema.Root!.Elements(s_xs + "simpleType").Single(e => (string?)e.Attribute("name") == simpleType);
        return (string)type.Descendants(s_xs + "pattern").Single().Attribute("value")!;
    }
}
