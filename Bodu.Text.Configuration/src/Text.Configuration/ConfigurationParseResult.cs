// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Text.Configuration;

/// <summary>
/// Carries the outcome of a configuration parse: the populated <see cref="ConfigurationDocument" /> and any diagnostics
/// collected during the parse.
/// </summary>
/// <remarks>
/// Under <see cref="ConfigurationDiagnosticMode.Throw" /> the parser raises <see cref="ConfigurationParseException" />
/// on the first recoverable error, so a returned <see cref="ConfigurationParseResult" /> from
/// <see cref="ConfigurationDocument.ParseWithDiagnostics" /> always represents a successful parse whose diagnostic list
/// is empty. Under <see cref="ConfigurationDiagnosticMode.Collect" /> the parser runs to completion and the diagnostic
/// list reflects every recoverable error and warning encountered.
/// </remarks>
public sealed class ConfigurationParseResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseResult" /> class.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <param name="diagnostics">The diagnostics collected during the parse.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public ConfigurationParseResult(ConfigurationDocument document, ImmutableArray<ConfigurationDiagnostic> diagnostics)
    {
        ThrowHelper.ThrowIfNull(document);
        Document = document;
        Diagnostics = diagnostics.IsDefault ? [] : diagnostics;
    }

    /// <summary>
    /// Gets the parsed document.
    /// </summary>
    /// <value>A populated <see cref="ConfigurationDocument" />.</value>
    public ConfigurationDocument Document { get; }

    /// <summary>
    /// Gets the diagnostics collected during the parse.
    /// </summary>
    /// <value>An immutable, possibly empty array of diagnostics.</value>
    public ImmutableArray<ConfigurationDiagnostic> Diagnostics { get; }
}
