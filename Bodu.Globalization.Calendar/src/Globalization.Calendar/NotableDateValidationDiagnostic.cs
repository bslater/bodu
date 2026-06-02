// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnostic.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Classifies the seriousness of a <see cref="NotableDateValidationDiagnostic" />.
/// </summary>
public enum NotableDateValidationSeverity
{
    /// <summary>
    /// A non-fatal authoring concern that does not prevent resolution (for example, an unregistered algorithm key that
    /// may be supplied by an optional pack).
    /// </summary>
    Warning,

    /// <summary>
    /// A fatal authoring error that will cause incorrect or failed resolution (for example, a missing or ambiguous
    /// offset anchor).
    /// </summary>
    Error,
}

/// <summary>
/// Describes a single rule-set validation finding produced by the strict-validation pass.
/// </summary>
/// <param name="Severity">The seriousness of the finding.</param>
/// <param name="Code">A stable, machine-readable code identifying the kind of finding (for example, <c>MissingAnchor</c>).</param>
/// <param name="Message">A human-readable description of the finding.</param>
public sealed record NotableDateValidationDiagnostic(
    NotableDateValidationSeverity Severity,
    string Code,
    string Message);
