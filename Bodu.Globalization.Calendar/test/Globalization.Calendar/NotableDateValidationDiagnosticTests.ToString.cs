// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnosticTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateValidationDiagnosticTests
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

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.ToString" /> appends the line and the position on the
    /// line in parentheses when the diagnostic carries both.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLineNumberAndLinePositionAreSet_ShouldAppendBoth()
    {
        NotableDateValidationDiagnostic diagnostic = CreateDiagnostic() with { LineNumber = 12, LinePosition = 7 };

        Assert.AreEqual("[Error] BODU-CAL-TEST: something failed (line 12, position 7)", diagnostic.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.ToString" /> appends the line alone when the diagnostic
    /// carries a line but no position on it.
    /// </summary>
    [TestMethod]
    public void ToString_WhenOnlyLineNumberIsSet_ShouldAppendTheLine()
    {
        NotableDateValidationDiagnostic diagnostic = CreateDiagnostic() with { LineNumber = 12 };

        Assert.AreEqual("[Error] BODU-CAL-TEST: something failed (line 12)", diagnostic.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.ToString" /> renders a position without a line as if
    /// the diagnostic carried no position.
    /// </summary>
    [TestMethod]
    public void ToString_WhenOnlyLinePositionIsSet_ShouldOmitThePosition()
    {
        NotableDateValidationDiagnostic diagnostic = CreateDiagnostic() with { LinePosition = 7 };

        Assert.AreEqual("[Error] BODU-CAL-TEST: something failed", diagnostic.ToString());
    }
}
