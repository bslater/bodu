// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Parses a notable-date document XML document into its component model objects, validating it against the embedded XSD
/// and delegating the structural shaping to <see cref="NotableDateDocumentReader" />.
/// </summary>
internal static class NotableDateDocumentParser
{
    /// <summary>The manifest resource name of the embedded notable-date document XSD.</summary>
    private const string SchemaResourceName = "Bodu.Globalization.Calendar.NotableDates.xsd";

    /// <summary>The maximum number of characters an untrusted notable-date document may contain (64 MiB).</summary>
    private const long MaxDocumentCharacters = 64L * 1024 * 1024;

    /// <summary>The XML namespace of the notable-date document vocabulary.</summary>
    private static readonly XNamespace s_ns = "urn:bodu:globalization:calendar";

    /// <summary>The reader settings used to materialize an untrusted document. DTD and entity processing are prohibited — blocking XXE and entity-expansion (billion-laughs) attacks explicitly rather than relying on the framework default — no external resolver is used, and the total document size is bounded.</summary>
    private static readonly XmlReaderSettings s_readerSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 0,
        MaxCharactersInDocument = MaxDocumentCharacters,
        CloseInput = true,
    };

    /// <summary>
    /// Parses and schema-validates a notable-date document XML document.
    /// </summary>
    /// <param name="xml">The notable-date document XML content.</param>
    /// <param name="diagnostics">The collection that receives structural and semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ParsedNotableDateDocument" />.</returns>
    /// <exception cref="FormatException"><paramref name="xml" /> is not well-formed XML.</exception>
    public static ParsedNotableDateDocument Parse(string xml, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        XDocument document;
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), s_readerSettings);
            document = XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_XmlNotWellFormed, ex.Message),
                ex);
        }

        ValidateSchema(document, diagnostics);

        XElement root = document.Root ?? new XElement(s_ns + "NotableDateResource");

        return NotableDateDocumentReader.Read(new XmlDocumentNode(root, s_ns), diagnostics);
    }

    /// <summary>
    /// Validates the document against the embedded XSD, recording each schema violation as a diagnostic.
    /// </summary>
    /// <param name="document">The parsed XML document.</param>
    /// <param name="diagnostics">The collection that receives schema diagnostics.</param>
    private static void ValidateSchema(XDocument document, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        using Stream? schemaStream = typeof(NotableDateDocumentParser).Assembly.GetManifestResourceStream(SchemaResourceName);
        if (schemaStream is null)
            return;

        XmlSchemaSet schemas = new();
        using (var schemaReader = XmlReader.Create(schemaStream))
            schemas.Add(s_ns.NamespaceName, schemaReader);

        document.Validate(schemas, (sender, e) =>
        {
            NotableDateValidationSeverity severity = e.Severity == XmlSeverityType.Error
                ? NotableDateValidationSeverity.Error
                : NotableDateValidationSeverity.Warning;

            diagnostics.Add(new NotableDateValidationDiagnostic(
                severity,
                "BODU-CAL-SCHEMA",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_XmlSchema, e.Message)));
        });
    }
}
