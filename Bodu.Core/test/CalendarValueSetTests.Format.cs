// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetTests.Format.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public sealed partial class CalendarValueSetTests
{
    /// <summary>
    /// Verifies that the list form of a 64-value domain writes the runs that reach bit 63 whole.
    /// </summary>
    /// <param name="bits">The bitmap.</param>
    /// <param name="minimum">The value bit zero selects.</param>
    /// <param name="expected">The expected list.</param>
    [TestMethod]
    [DataRow(ulong.MaxValue, 0, "0-63")]
    [DataRow(ulong.MaxValue, 1, "1-64")]
    [DataRow(0x8000_0000_0000_0000UL, 0, "63")]
    [DataRow(0x8000_0000_0000_0001UL, 0, "0,63")]
    [DataRow(0xC000_0000_0000_0003UL, 0, "0-1,62-63")]
    [DataRow(0x7FFF_FFFF_FFFF_FFFEUL, 0, "1-62")]
    public void Format_WhenTheDomainHasSixtyFourValues_ShouldWriteTheRunsThatReachTheTopBit(ulong bits, int minimum, string expected)
    {
        Assert.AreEqual(expected, CalendarValueSet.Format(bits, minimum));
    }

    /// <summary>
    /// Verifies that the list form of every sample bitmap over a 64-value domain reads back to the same bitmap.
    /// </summary>
    [TestMethod]
    public void Format_WhenTheDomainHasSixtyFourValues_ShouldReadBackThroughTryParse()
    {
        foreach (ulong bits in SampleBits())
        {
            string text = CalendarValueSet.Format(bits, 0);

            Assert.IsTrue(CalendarValueSet.TryParse(text, 0, 63, out ulong parsed), text);
            Assert.AreEqual(bits, parsed, text);
        }
    }

    /// <summary>
    /// Verifies that the longest list the text buffer allows, every value of a 64-value domain with three digits each,
    /// is written whole: 64 values and 63 commas.
    /// </summary>
    [TestMethod]
    public void FormatValues_WhenEveryValueHasThreeDigits_ShouldWriteTheLongestList()
    {
        string expected = string.Join(",", Enumerable.Range(936, 64));

        string text = CalendarValueSet.FormatValues(ulong.MaxValue, 936);

        Assert.AreEqual(expected, text);
        Assert.AreEqual(CalendarValueSet.MaxTextLength - 1, text.Length);
    }

    /// <summary>
    /// Verifies that the list parser rejects a value one past a 64-value domain at either end.
    /// </summary>
    /// <param name="text">The list text.</param>
    [TestMethod]
    [DataRow("64")]
    [DataRow("0-64")]
    [DataRow("63-64")]
    public void TryParse_WhenAValueIsPastASixtyFourValueDomain_ShouldReturnFalse(string text)
    {
        Assert.IsFalse(CalendarValueSet.TryParse(text, 0, 63, out ulong bits), text);
        Assert.AreEqual(0UL, bits, text);
    }
}
