// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformationTests.TotalEditTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class SummaryInformationTests
{
    /// <summary>
    /// Verifies that the total edit time, a <c>VT_FILETIME</c> that [MS-OLEPS] gives PIDSI_EDITTIME as a duration in
    /// 100-nanosecond units, reads as that duration rather than as an instant.
    /// </summary>
    [TestMethod]
    public void TotalEditTime_WhenFileTimeHoldsADuration_ShouldReturnTimeSpan()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (10, PropertySetBytes.Typed(0x0040, PropertySetBytes.Int64(36000000000))));

        var summary = new SummaryInformation(OlePropertySet.Parse(bytes));

        Assert.AreEqual(TimeSpan.FromHours(1), summary.TotalEditTime);
    }
}
