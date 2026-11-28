// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnosticTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the position, rendering and equality of <see cref="NotableDateValidationDiagnostic" />.
/// </summary>
[TestClass]
public partial class NotableDateValidationDiagnosticTests
{
    /// <summary>
    /// Creates a diagnostic with a fixed severity, code and message.
    /// </summary>
    /// <returns>A diagnostic that carries no position.</returns>
    private static NotableDateValidationDiagnostic CreateDiagnostic() =>
        new(NotableDateValidationSeverity.Error, "BODU-CAL-TEST", "something failed");
}
