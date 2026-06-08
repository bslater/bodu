// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateJsonDocumentParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Parses a notable-date document expressed in JSON into its component model objects, delegating the structural shaping
/// to <see cref="NotableDateDocumentReader" />. The JSON shape mirrors the XML vocabulary one-to-one (camelCase
/// strategy discriminators, a <c>rules[].adjustments</c> array of policy identifiers).
/// </summary>
internal static class NotableDateJsonDocumentParser
{
    /// <summary>
    /// Parses a notable-date document from its JSON content.
    /// </summary>
    /// <param name="json">The notable-date document JSON content.</param>
    /// <param name="diagnostics">The collection that receives structural and semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ParsedNotableDateDocument" />.</returns>
    /// <exception cref="FormatException"><paramref name="json" /> is not well-formed JSON.</exception>
    public static ParsedNotableDateDocument Parse(string json, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        using JsonDocument document = ParseDocument(json);

        return NotableDateDocumentReader.Read(new JsonDocumentNode(document.RootElement), diagnostics);
    }

    /// <summary>
    /// Parses the raw JSON text, wrapping a syntax error as a <see cref="FormatException" />.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The parsed document.</returns>
    private static JsonDocument ParseDocument(string json)
    {
        // System.Text.Json rejects a leading byte-order mark, which a UTF-8 string loaded from a BOM-prefixed source can
        // carry; strip it so such content parses rather than failing on its first byte.
        if (json.Length > 0 && json[0] == '\uFEFF')
            json = json[1..];

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_JsonNotWellFormed, ex.Message),
                ex);
        }
    }
}
