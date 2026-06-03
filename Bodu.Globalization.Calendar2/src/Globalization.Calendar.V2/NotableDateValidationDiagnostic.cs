// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnostic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents a single diagnostic produced while loading and validating a notable-date document resource.
/// </summary>
/// <param name="Severity">The severity of the diagnostic.</param>
/// <param name="Code">A short, stable code identifying the kind of diagnostic.</param>
/// <param name="Message">A human-readable description of the diagnostic.</param>
public sealed record NotableDateValidationDiagnostic(
    NotableDateValidationSeverity Severity,
    string Code,
    string Message)
{
    /// <summary>
    /// Returns a readable rendering of the diagnostic, combining its severity, code, and message.
    /// </summary>
    /// <returns>The diagnostic formatted as <c>[Severity] Code: Message</c>.</returns>
    public override string ToString() =>
        $"[{this.Severity}] {this.Code}: {this.Message}";
}
