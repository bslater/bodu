// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnosticTests.LineNumber.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateValidationDiagnosticTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.LineNumber" /> is <see langword="null" /> on a
    /// diagnostic constructed without a position.
    /// </summary>
    [TestMethod]
    public void LineNumber_WhenNotSet_ShouldBeNull() =>
        Assert.IsNull(CreateDiagnostic().LineNumber);

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.LineNumber" /> returns the line it was initialized
    /// with.
    /// </summary>
    [TestMethod]
    public void LineNumber_WhenInitialized_ShouldReturnTheLine() =>
        Assert.AreEqual(3, (CreateDiagnostic() with { LineNumber = 3 }).LineNumber);

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.LineNumber" /> rejects a line below one, since lines
    /// are counted from one, with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="line">The line below one.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void LineNumber_WhenInitializedBelowOne_ShouldThrowArgumentOutOfRangeException(int line)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CreateDiagnostic() with { LineNumber = line };
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
