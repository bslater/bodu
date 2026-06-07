// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the entry points for loading a notable-date document from XML or JSON into a validated
/// <see cref="NotableDateResource" />.
/// </summary>
/// <remarks>
/// <para>
/// Loading runs the deterministic pipeline: parse the document, resolve its imports against a supplied resource
/// resolver, apply ID-targeted override operations, assemble the resource, then run semantic validation. When any
/// error-severity diagnostic is produced the load fails with a <see cref="NotableDateValidationException" /> that
/// carries the full diagnostic set.
/// </para>
/// <para>
/// The overloads without a resolver cannot satisfy imports; a document declaring <c>Imports</c> must be loaded with the
/// resolver overload, whose delegate maps a resource name to that resource's XML or JSON content.
/// </para>
/// <para>
/// <strong>When to use.</strong> Call <see cref="Load(string)" /> / <see cref="LoadJson(string)" /> (or the
/// <see cref="Stream" /> overloads) for a self-contained document, and the resolver overloads when the document imports
/// shared concepts or adjustment policies from other resources. For the bundled territory packs prefer the data-pack
/// factories (for example the <c>AmericasCalendarData</c> bundle), which load and wire the embedded resources for you.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A self-contained document with no imports.
/// NotableDateResource resource = NotableDateResourceLoader.Load(documentXml);
/// NotableDateService service = new(resource);
///
/// // A document that imports shared concepts: map each import name to its content.
/// var library = new Dictionary<string, string>(StringComparer.Ordinal)
/// {
///     ["common-christian"] = christianXml,
/// };
/// NotableDateResource composed = NotableDateResourceLoader.Load(
///     documentXml,
///     name => library.TryGetValue(name, out string? content) ? content : null);
///
/// // The same pipeline accepts the JSON projection of the schema.
/// NotableDateResource fromJson = NotableDateResourceLoader.LoadJson(documentJson);
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateResource" /> <seealso cref="NotableDateService" />
/// <seealso cref="NotableDateValidationException" /> <seealso href="../guides/calendar/building-the-service.html">
/// Building and extending the service (guide)</seealso>
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
    public static NotableDateResource Load(string xml) =>
        Load(xml, NoResolver);

    /// <summary>
    /// Loads and validates a notable-date document from its XML content, resolving imports through the supplied
    /// resolver.
    /// </summary>
    /// <param name="xml">The notable-date document XML content.</param>
    /// <param name="resourceResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> when missing.
    /// </param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="xml" /> or <paramref name="resourceResolver" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="xml" /> is empty or white-space.</exception>
    /// <exception cref="FormatException"><paramref name="xml" /> is not well-formed XML.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource Load(string xml, Func<string, string?> resourceResolver) =>
        Load(xml, resourceResolver, null);

    /// <summary>
    /// Loads and validates a notable-date document from its XML content, resolving imports through the supplied
    /// resolver and accepting custom algorithm keys registered in the supplied registry.
    /// </summary>
    /// <param name="xml">The notable-date document XML content.</param>
    /// <param name="resourceResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> when missing.
    /// </param>
    /// <param name="algorithms">
    /// A registry of custom algorithm keys to accept during validation, or <see langword="null" />.
    /// </param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="xml" /> or <paramref name="resourceResolver" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="xml" /> is empty or white-space.</exception>
    /// <exception cref="FormatException"><paramref name="xml" /> is not well-formed XML.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource Load(string xml, Func<string, string?> resourceResolver, INotableDateAlgorithmRegistry? algorithms)
    {
        ThrowHelper.ThrowIfNull(xml);
        ThrowHelper.ThrowIfNull(resourceResolver);
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_DocumentContentEmpty, nameof(xml));

        List<NotableDateValidationDiagnostic> diagnostics = new();
        ParsedNotableDateDocument document = NotableDateDocumentParser.Parse(xml, diagnostics);

        return Build(document, resourceResolver, algorithms, diagnostics);
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
    public static NotableDateResource Load(Stream stream) =>
        Load(ReadToEnd(stream));

    /// <summary>
    /// Loads and validates a notable-date document from a stream of XML content, resolving imports through the supplied
    /// resolver.
    /// </summary>
    /// <param name="stream">The stream containing the notable-date document XML.</param>
    /// <param name="resourceResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> when missing.
    /// </param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream" /> or <paramref name="resourceResolver" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">The stream content is empty or white-space.</exception>
    /// <exception cref="FormatException">The stream content is not well-formed XML.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource Load(Stream stream, Func<string, string?> resourceResolver) =>
        Load(ReadToEnd(stream), resourceResolver);

    /// <summary>
    /// Loads and validates a notable-date document from its JSON content.
    /// </summary>
    /// <param name="json">The notable-date document JSON content.</param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="json" /> is empty or white-space.</exception>
    /// <exception cref="FormatException"><paramref name="json" /> is not well-formed JSON.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource LoadJson(string json) =>
        LoadJson(json, NoResolver);

    /// <summary>
    /// Loads and validates a notable-date document from its JSON content, resolving imports through the supplied
    /// resolver.
    /// </summary>
    /// <param name="json">The notable-date document JSON content.</param>
    /// <param name="resourceResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> when missing.
    /// </param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="json" /> or <paramref name="resourceResolver" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="json" /> is empty or white-space.</exception>
    /// <exception cref="FormatException"><paramref name="json" /> is not well-formed JSON.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource LoadJson(string json, Func<string, string?> resourceResolver)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(resourceResolver);
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_DocumentContentEmpty, nameof(json));

        List<NotableDateValidationDiagnostic> diagnostics = new();
        ParsedNotableDateDocument document = NotableDateJsonDocumentParser.Parse(json, diagnostics);

        return Build(document, resourceResolver, null, diagnostics);
    }

    /// <summary>
    /// Loads and validates a notable-date document from a stream of JSON content.
    /// </summary>
    /// <param name="stream">The stream containing the notable-date document JSON.</param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The stream content is empty or white-space.</exception>
    /// <exception cref="FormatException">The stream content is not well-formed JSON.</exception>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    public static NotableDateResource LoadJson(Stream stream) =>
        LoadJson(ReadToEnd(stream));

    /// <summary>
    /// Resolves imports, applies overrides, assembles the resource, runs semantic validation, and fails when any error
    /// diagnostic is produced.
    /// </summary>
    /// <param name="document">The parsed root document.</param>
    /// <param name="resourceResolver">The resolver used to fetch imported resources.</param>
    /// <param name="algorithms">
    /// A registry of custom algorithm keys to accept during validation, or <see langword="null" />.
    /// </param>
    /// <param name="diagnostics">
    /// The diagnostics accumulated during parsing, extended by import resolution and validation.
    /// </param>
    /// <returns>The validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="NotableDateValidationException">
    /// The notable-date document produced one or more error diagnostics.
    /// </exception>
    private static NotableDateResource Build(ParsedNotableDateDocument document, Func<string, string?> resourceResolver, INotableDateAlgorithmRegistry? algorithms, List<NotableDateValidationDiagnostic> diagnostics)
    {
        (List<AdjustmentPolicy> policies, List<NotableDateDefinition> concepts) =
            ResolveImports(document, resourceResolver, new HashSet<string>(StringComparer.Ordinal), diagnostics);

        List<NotableDateDefinition> definitions = NotableDateRuleOverrideApplier.Apply(concepts, document.Overrides, diagnostics);

        NotableDateResource resource = new(
            document.ResourceId,
            document.SchemaVersion,
            document.ResolutionPolicy,
            policies,
            definitions);

        NotableDateRuleValidator.Validate(resource, diagnostics, algorithms);

        var errorCount = diagnostics.Count(d => d.Severity == NotableDateValidationSeverity.Error);
        if (errorCount > 0)
        {
            throw new NotableDateValidationException(
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Op_Invalid_DocumentValidationFailed, errorCount),
                diagnostics);
        }

        return resource;
    }

    /// <summary>
    /// Recursively resolves the document's imports, merging imported adjustment policies and concepts with the local
    /// ones; local concepts win over imported concepts with the same identifier.
    /// </summary>
    /// <param name="document">The document whose imports are being resolved.</param>
    /// <param name="resourceResolver">The resolver used to fetch imported resources.</param>
    /// <param name="visiting">The set of resource names currently being resolved, used to detect cycles.</param>
    /// <param name="diagnostics">The collection that receives import diagnostics.</param>
    /// <returns>The merged adjustment policies and notable-date concepts.</returns>
    private static (List<AdjustmentPolicy> Policies, List<NotableDateDefinition> Concepts) ResolveImports(
        ParsedNotableDateDocument document,
        Func<string, string?> resourceResolver,
        HashSet<string> visiting,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<AdjustmentPolicy> policies = new(document.AdjustmentPolicies);
        HashSet<string> policyIds = new(policies.Select(p => p.Id), StringComparer.Ordinal);

        List<NotableDateDefinition> imported = new();
        HashSet<string> importedIds = new(StringComparer.Ordinal);

        foreach (NotableDateImport import in document.Imports)
        {
            if (!visiting.Add(import.Resource))
            {
                AddError(diagnostics, "BODU-CAL-IMPORT-CYCLE", CalendarResourceStrings.Validation_ImportCycle, import.Resource);
                continue;
            }

            try
            {
                var content = resourceResolver(import.Resource);
                if (content is null)
                {
                    AddError(diagnostics, "BODU-CAL-IMPORT-MISSING", CalendarResourceStrings.Validation_ImportResourceNotFound, import.Resource);
                    continue;
                }

                ParsedNotableDateDocument source = ParseAny(content, diagnostics);
                (List<AdjustmentPolicy> sourcePolicies, List<NotableDateDefinition> sourceConcepts) =
                    ResolveImports(source, resourceResolver, visiting, diagnostics);

                foreach (AdjustmentPolicy policy in sourcePolicies)
                {
                    if (policyIds.Add(policy.Id))
                        policies.Add(policy);
                }

                foreach (NotableDateDefinition concept in SelectImportedConcepts(import, sourceConcepts, diagnostics))
                {
                    if (importedIds.Add(concept.Id))
                        imported.Add(concept);
                }
            }
            finally
            {
                // Leave the in-progress set exactly as it was found so a resource imported on two distinct branches is
                // not mistaken for a cycle, regardless of how the body above exits.
                visiting.Remove(import.Resource);
            }
        }

        HashSet<string> localIds = new(document.NotableDates.Select(d => d.Id), StringComparer.Ordinal);
        var concepts = imported.Where(c => !localIds.Contains(c.Id)).ToList();
        concepts.AddRange(document.NotableDates);

        return (policies, concepts);
    }

    /// <summary>
    /// Selects the concepts an import contributes: every concept of the source when no cherry-picks are listed, or the
    /// named concepts with their per-import overrides applied.
    /// </summary>
    /// <param name="import">The import directive.</param>
    /// <param name="sourceConcepts">The fully resolved concepts of the imported source.</param>
    /// <param name="diagnostics">The collection that receives import diagnostics.</param>
    /// <returns>The selected concepts.</returns>
    private static IEnumerable<NotableDateDefinition> SelectImportedConcepts(
        NotableDateImport import,
        List<NotableDateDefinition> sourceConcepts,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (import.Uses.Count == 0)
        {
            foreach (NotableDateDefinition concept in sourceConcepts)
                yield return concept;

            yield break;
        }

        foreach (NotableDateImportUse use in import.Uses)
        {
            NotableDateDefinition? concept = sourceConcepts.FirstOrDefault(c => string.Equals(c.Id, use.NotableDateRef, StringComparison.Ordinal));
            if (concept is null)
            {
                AddError(diagnostics, "BODU-CAL-IMPORT-CONCEPT", CalendarResourceStrings.Validation_ImportConceptNotFound, import.Resource, use.NotableDateRef);
                continue;
            }

            yield return ApplyUse(concept, use);
        }
    }

    /// <summary>
    /// Applies an import use's rename and overrides to a concept.
    /// </summary>
    /// <param name="concept">The concept being imported.</param>
    /// <param name="use">The cherry-pick directive.</param>
    /// <returns>The renamed and overridden concept.</returns>
    private static NotableDateDefinition ApplyUse(NotableDateDefinition concept, NotableDateImportUse use)
    {
        var id = string.IsNullOrEmpty(use.As) ? concept.Id : use.As!;
        NotableDateCategory category = use.Category ?? concept.Category;
        var nonWorking = use.NonWorking ?? concept.DefaultNonWorkingDay;

        var overrideTerritory = !string.IsNullOrEmpty(use.Territory);
        var overrideAdjustments = use.AdjustmentPolicyRefs is not null;

        IReadOnlyList<NotableDateRule> rules = concept.Rules;
        if (overrideTerritory || overrideAdjustments)
        {
            rules = [.. concept.Rules
                .Select(r => new NotableDateRule(
                    r.Id,
                    r.Priority,
                    r.Category,
                    r.NonWorking,
                    r.DurationDays,
                    overrideTerritory
                        ? new RuleApplicability(
                            r.Applicability.Calendar,
                            r.Applicability.FromYear,
                            r.Applicability.ToYear,
                            [use.Territory!],
                            r.Applicability.OnlyYears,
                            r.Applicability.ExceptYears,
                            r.Applicability.EveryYears,
                            r.Applicability.AnchorYear)
                        : r.Applicability,
                    r.Strategy,
                    overrideAdjustments ? use.AdjustmentPolicyRefs! : r.AdjustmentPolicyRefs,
                    r.Tags))];
        }

        return new NotableDateDefinition(id, concept.DisplayName, category, nonWorking, concept.DefaultDurationDays, concept.Tags, rules);
    }

    /// <summary>
    /// Parses imported content as JSON when its first significant character opens a JSON value, otherwise as XML.
    /// </summary>
    /// <param name="content">The resource content.</param>
    /// <param name="diagnostics">The collection that receives parse diagnostics.</param>
    /// <returns>The parsed document.</returns>
    private static ParsedNotableDateDocument ParseAny(string content, ICollection<NotableDateValidationDiagnostic> diagnostics) =>
        LooksLikeJson(content)
            ? NotableDateJsonDocumentParser.Parse(content, diagnostics)
            : NotableDateDocumentParser.Parse(content, diagnostics);

    /// <summary>
    /// Determines whether content is JSON rather than XML by inspecting its first significant character, skipping a
    /// leading byte-order mark and any white-space so a BOM-prefixed or indented document is not misrouted.
    /// </summary>
    /// <param name="content">The resource content.</param>
    /// <returns>
    /// <see langword="true" /> when the first significant character opens a JSON object or array; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool LooksLikeJson(string content)
    {
        foreach (var character in content)
        {
            if (character == '\uFEFF' || char.IsWhiteSpace(character))
                continue;

            return character is '{' or '[';
        }

        return false;
    }

    /// <summary>
    /// Adds an error diagnostic formatted from a resource string.
    /// </summary>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="format">The resource format string.</param>
    /// <param name="args">The format arguments.</param>
    private static void AddError(ICollection<NotableDateValidationDiagnostic> diagnostics, string code, string format, params object[] args) =>
        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            code,
            string.Format(CultureInfo.InvariantCulture, format, args)));

    /// <summary>
    /// Reads a stream to the end as UTF-8 text, leaving the stream open.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The decoded content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    private static string ReadToEnd(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// A resolver that resolves no resources, used by the overloads that do not accept one.
    /// </summary>
    /// <param name="resource">The requested resource name.</param>
    /// <returns>Always <see langword="null" />.</returns>
    private static string? NoResolver(string resource) =>
        null;
}
