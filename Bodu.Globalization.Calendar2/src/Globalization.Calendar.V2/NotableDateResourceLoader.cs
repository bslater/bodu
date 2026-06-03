// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Provides the entry points for loading a notable-date document from XML into a validated
/// <see cref="NotableDateResource" />.
/// </summary>
/// <remarks>
/// <para>
/// Loading runs the deterministic pipeline: parse and schema-validate the XML, apply ID-targeted override operations,
/// assemble the resource, then run semantic validation. When any error-severity diagnostic is produced the load fails
/// with a <see cref="NotableDateValidationException" /> that carries the full diagnostic set.
/// </para>
/// </remarks>
public static class NotableDateResourceLoader
{
    /// <summary>
    /// Loads and validates a notable-date document from its XML content.
    /// </summary>
    /// <param name="xml">The notable-date document XML content.</param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="xml" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="xml" /> is empty or white-space.</exception>
    /// <exception cref="FormatException"><paramref name="xml" /> is not well-formed XML.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource Load(string xml)
    {
        ThrowHelper.ThrowIfNull(xml);
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException(Calendar2ResourceStrings.Arg_Invalid_DocumentContentEmpty, nameof(xml));

        List<NotableDateValidationDiagnostic> diagnostics = new();

        ParsedNotableDateDocument document = NotableDateDocumentParser.Parse(xml, diagnostics);
        List<NotableDateDefinition> definitions = NotableDateRuleOverrideApplier.Apply(document.NotableDates, document.Overrides, diagnostics);

        NotableDateResource resource = new(
            document.ResourceId,
            document.SchemaVersion,
            document.ResolutionPolicy,
            document.AdjustmentPolicies,
            definitions);

        NotableDateRuleValidator.Validate(resource, diagnostics);

        int errorCount = diagnostics.Count(d => d.Severity == NotableDateValidationSeverity.Error);
        if (errorCount > 0)
        {
            throw new NotableDateValidationException(
                string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Op_Invalid_DocumentValidationFailed, errorCount),
                diagnostics);
        }

        return resource;
    }

    /// <summary>
    /// Loads and validates a notable-date document from a stream of XML content.
    /// </summary>
    /// <param name="stream">The stream containing the notable-date document XML.</param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The stream content is empty or white-space.</exception>
    /// <exception cref="FormatException">The stream content is not well-formed XML.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource Load(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        return Load(reader.ReadToEnd());
    }
}
