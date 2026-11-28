// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnosticTests.Equals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateValidationDiagnosticTests
{
    /// <summary>
    /// Verifies that two diagnostics with the same severity, code, message and position are equal and share a hash
    /// code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenPositionsMatch_ShouldReturnTrue()
    {
        NotableDateValidationDiagnostic first = CreateDiagnostic() with { LineNumber = 4, LinePosition = 6 };
        NotableDateValidationDiagnostic second = CreateDiagnostic() with { LineNumber = 4, LinePosition = 6 };

        Assert.AreEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that a diagnostic carrying a position is not equal to one carrying the same text without it.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOnlyOneCarriesAPosition_ShouldReturnFalse() =>
        Assert.AreNotEqual(CreateDiagnostic(), CreateDiagnostic() with { LineNumber = 4, LinePosition = 6 });

    /// <summary>
    /// Verifies that two diagnostics with the same text at different positions on the same line are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenLinePositionsDiffer_ShouldReturnFalse() =>
        Assert.AreNotEqual(CreateDiagnostic() with { LineNumber = 4, LinePosition = 6 }, CreateDiagnostic() with { LineNumber = 4, LinePosition = 7 });
}
