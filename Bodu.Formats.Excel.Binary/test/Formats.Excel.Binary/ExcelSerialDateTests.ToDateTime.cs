// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelSerialDateTests.ToDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class ExcelSerialDateTests
{
    /// <summary>
    /// Verifies that the date and time conversion preserves a fractional time-of-day component.
    /// </summary>
    [TestMethod]
    public void ToDateTime_WhenSerialHasFraction_ShouldPreserveTimeOfDay()
    {
        var result = ExcelSerialDate.ToDateTime(44929.5);

        Assert.AreEqual(new DateTime(2023, 1, 3, 12, 0, 0), result);
    }
}
