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
/// Describes a single rule-set validation finding produced by the strict-validation pass run by
/// <see cref="NotableDateService.Validate" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Code" /> is stable across releases so callers can branch on it. The pass emits the following codes:
/// </para>
/// <list type="table">
/// <listheader><term>Code</term><description>Severity and meaning</description></listheader>
/// <item><term><c>DuplicateIdentity</c></term><description>Error — two rules share the same full identity (name, variant, territory, calendar).</description></item>
/// <item><term><c>MissingAnchor</c></term><description>Error — an <c>OffsetFromAnchor</c> rule references an anchor that does not exist.</description></item>
/// <item><term><c>AmbiguousAnchor</c></term><description>Error — an anchor reference matches more than one rule and cannot be narrowed by context.</description></item>
/// <item><term><c>MissingReplacementTarget</c></term><description>Error — a <c>ReplaceWithNamedDate</c> adjustment targets a rule that does not exist.</description></item>
/// <item><term><c>AmbiguousReplacementTarget</c></term><description>Error — a replacement target matches more than one rule.</description></item>
/// <item><term><c>UnregisteredAlgorithm</c></term><description>Warning — an <c>Algorithm</c> rule names a key absent from the registry (an optional pack may supply it later).</description></item>
/// </list>
/// </remarks>
/// <param name="Severity">The seriousness of the finding.</param>
/// <param name="Code">A stable, machine-readable code identifying the kind of finding (for example, <c>MissingAnchor</c>).</param>
/// <param name="Message">A human-readable description of the finding.</param>
public sealed record NotableDateValidationDiagnostic(
    NotableDateValidationSeverity Severity,
    string Code,
    string Message);
