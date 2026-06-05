// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarBuilderInvalidKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Represents a negative-vector known-answer test row for the <c>Bodu.Globalization.Calendar.Builder</c> source
/// generator: a malformed source document and the diagnostic identifier the builder must report.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="SourceInput">The malformed rule document supplied to the builder.</param>
/// <param name="ExpectedDiagnosticId">
/// The diagnostic identifier the builder must surface (for example <c>"BC001"</c>).
/// </param>
/// <param name="MessageContains">An optional substring the diagnostic message must contain.</param>
public sealed record CalendarBuilderInvalidKat(
    string Name,
    string SourceInput,
    string ExpectedDiagnosticId,
    string? MessageContains = null) : IKat;
