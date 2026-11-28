// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnosticTests.LinePosition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateValidationDiagnosticTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.LinePosition" /> is <see langword="null" /> on a
    /// diagnostic constructed without a position.
    /// </summary>
    [TestMethod]
    public void LinePosition_WhenNotSet_ShouldBeNull() =>
        Assert.IsNull(CreateDiagnostic().LinePosition);

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.LinePosition" /> returns the position it was
    /// initialized with.
    /// </summary>
    [TestMethod]
    public void LinePosition_WhenInitialized_ShouldReturnThePosition() =>
        Assert.AreEqual(9, (CreateDiagnostic() with { LinePosition = 9 }).LinePosition);

    /// <summary>
    /// Verifies that <see cref="NotableDateValidationDiagnostic.LinePosition" /> rejects a position below one, since
    /// positions are counted from one, with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="position">The position below one.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void LinePosition_WhenInitializedBelowOne_ShouldThrowArgumentOutOfRangeException(int position)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CreateDiagnostic() with { LinePosition = position };
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
