// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.Validation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>The diagnostic code reporting a document too incomplete to serialize for validation.</summary>
    private const string BuilderIncompleteCode = "BODU-CAL-BUILDER-INCOMPLETE";

    /// <summary>
    /// Lints the document, returning every diagnostic the canonical loader's validation would produce — errors,
    /// warnings, and informational messages — without throwing.
    /// </summary>
    /// <returns>The collected diagnostics; empty when the document is valid.</returns>
    /// <remarks>
    /// <para>
    /// Validation runs the same pipeline as <see cref="Build()" /> — the document is serialized to XML and passed
    /// through <see cref="NotableDateResourceLoader" /> — so a clean result guarantees <see cref="Build()" /> succeeds
    /// for the same document. A document too incomplete to serialize (a missing resource identifier, a concept with no
    /// rules, a rule with no strategy) is reported as a <c>BODU-CAL-BUILDER-INCOMPLETE</c> error diagnostic rather
    /// than an exception.
    /// </para>
    /// </remarks>
    public IReadOnlyList<NotableDateValidationDiagnostic> Validate() =>
        Validate(null);

    /// <summary>
    /// Lints the document against the supplied import resolver, returning every diagnostic without throwing.
    /// </summary>
    /// <param name="importResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> to resolve no imports.
    /// </param>
    /// <returns>The collected diagnostics; empty when the document is valid.</returns>
    public IReadOnlyList<NotableDateValidationDiagnostic> Validate(Func<string, string?>? importResolver)
    {
        _ = TryBuild(importResolver, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        return diagnostics;
    }

    /// <summary>
    /// Attempts to materialize the document into a validated <see cref="NotableDateResource" />, collecting every
    /// diagnostic instead of throwing on an invalid document.
    /// </summary>
    /// <param name="resource">The built resource, or <see langword="null" /> when the document does not validate.</param>
    /// <param name="diagnostics">Every diagnostic serialization and validation produced.</param>
    /// <returns>
    /// <see langword="true" /> when the document built without error-severity diagnostics; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryBuild(out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics) =>
        TryBuild(null, out resource, out diagnostics);

    /// <summary>
    /// Attempts to materialize the document into a validated <see cref="NotableDateResource" /> resolving imports
    /// through the supplied resolver, collecting every diagnostic instead of throwing on an invalid document.
    /// </summary>
    /// <param name="importResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> to resolve no imports.
    /// </param>
    /// <param name="resource">The built resource, or <see langword="null" /> when the document does not validate.</param>
    /// <param name="diagnostics">Every diagnostic serialization and validation produced.</param>
    /// <returns>
    /// <see langword="true" /> when the document built without error-severity diagnostics; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryBuild(Func<string, string?>? importResolver, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics)
    {
        string xml;
        try
        {
            xml = ToXml();
        }
        catch (InvalidOperationException ex)
        {
            resource = null;
            diagnostics =
            [
                new NotableDateValidationDiagnostic(NotableDateValidationSeverity.Error, BuilderIncompleteCode, ex.Message),
            ];

            return false;
        }

        return NotableDateResourceLoader.TryLoad(xml, importResolver ?? (_ => null), out resource, out diagnostics);
    }
}
