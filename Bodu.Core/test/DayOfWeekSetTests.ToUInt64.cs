// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.ToUInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class DayOfWeekSetTests
{
    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToUInt64" /> puts each day at the bit of its <see cref="DayOfWeek" />
    /// value, Sunday at bit 0 and Saturday at bit 6, the reverse of the binary text form's left-to-right order.
    /// </summary>
    [TestMethod]
    [DataRow("Weekdays", 0b011_1110UL)]
    [DataRow("Weekend", 0b100_0001UL)]
    [DataRow("SundayToThursday", 0b001_1111UL)]
    [DataRow("SaturdayToWednesday", 0b100_1111UL)]
    [DataRow("MondayToThursdayAndSaturday", 0b101_1110UL)]
    [DataRow("All", 0b111_1111UL)]
    public void ToUInt64_WhenSetIsPreset_ShouldPutSundayAtBitZero(string preset, ulong expected)
    {
        var set = (DayOfWeekSet)typeof(DayOfWeekSet).GetProperty(preset)!.GetValue(null)!;

        Assert.AreEqual(expected, set.ToUInt64());
    }
}
