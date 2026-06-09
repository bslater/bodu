// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnosticTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the human-readable rendering of <see cref="NotableDateValidationDiagnostic" />.
/// </summary>
[TestClass]
public class NotableDateValidationDiagnosticTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.ToString" /> combines the severity, code, and message.
    /// </summary>
    [TestMethod]
    public void ToString_ShouldFormatSeverityCodeAndMessage()
    {
        var diagnostic = new NotableDateValidationDiagnostic(NotableDateValidationSeverity.Error, "BODU-CAL-TEST", "something failed");

        Assert.AreEqual("[Error] BODU-CAL-TEST: something failed", diagnostic.ToString());
    }
}
