// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSourcePositionTests.FromLineInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSourcePositionTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromLineInfo" /> keeps a line and position that an XML
    /// exception reports.
    /// </summary>
    [TestMethod]
    public void FromLineInfo_WhenALineIsReported_ShouldKeepIt() =>
        Assert.AreEqual(new NotableDateSourcePosition(4, 3), NotableDateSourcePosition.FromLineInfo(4, 3));

    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromLineInfo" /> returns <see langword="null" /> for the
    /// line zero an exception reports when it has no line information.
    /// </summary>
    [TestMethod]
    public void FromLineInfo_WhenNoLineIsReported_ShouldReturnNull() =>
        Assert.IsNull(NotableDateSourcePosition.FromLineInfo(0, 0));

    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromLineInfo" /> raises a reported position of zero to one,
    /// the first character of the line.
    /// </summary>
    [TestMethod]
    public void FromLineInfo_WhenThePositionIsZero_ShouldRaiseItToOne() =>
        Assert.AreEqual(new NotableDateSourcePosition(2, 1), NotableDateSourcePosition.FromLineInfo(2, 0));
}
