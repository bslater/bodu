// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetTests.Binary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public sealed partial class CalendarValueSetTests
{
    /// <summary>
    /// Verifies that the binary form writes one character per value, lowest first, <c>1</c> for a selected value.
    /// </summary>
    /// <param name="bits">The bitmap.</param>
    /// <param name="width">The number of values in the domain.</param>
    /// <param name="expected">The expected binary form.</param>
    [TestMethod]
    [DataRow(0UL, 7, "0000000")]
    [DataRow(0x3EUL, 7, "0111110")]
    [DataRow(0x1UL, 12, "100000000000")]
    [DataRow(0x800UL, 12, "000000000001")]
    public void FormatBinary_WhenWritten_ShouldPutTheLowestValueFirst(ulong bits, int width, string expected)
    {
        Assert.AreEqual(expected, CalendarValueSet.FormatBinary(bits, width));
    }

    /// <summary>
    /// Verifies that the binary form of a 64-value domain writes its top bit last and reads every sample back.
    /// </summary>
    [TestMethod]
    public void FormatBinary_WhenTheDomainHasSixtyFourValues_ShouldReadBackThroughTryParseBinary()
    {
        Assert.AreEqual(new string('0', 63) + "1", CalendarValueSet.FormatBinary(1UL << 63, 64));
        Assert.AreEqual(new string('1', 64), CalendarValueSet.FormatBinary(ulong.MaxValue, 64));

        foreach (ulong bits in SampleBits())
        {
            string text = CalendarValueSet.FormatBinary(bits, 64);

            Assert.AreEqual(CalendarValueSet.ParseFailure.None, CalendarValueSet.TryParseBinary(text, 64, out ulong parsed, out _), text);
            Assert.AreEqual(bits, parsed, text);
        }
    }

    /// <summary>
    /// Verifies that the binary reader reports a text of another length as a length failure.
    /// </summary>
    /// <param name="text">The text to read.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("011111")]
    [DataRow("01111100")]
    public void TryParseBinary_WhenTheLengthDiffers_ShouldReportLength(string text)
    {
        Assert.AreEqual(CalendarValueSet.ParseFailure.Length, CalendarValueSet.TryParseBinary(text, 7, out ulong bits, out _));
        Assert.AreEqual(0UL, bits);
    }

    /// <summary>
    /// Verifies that the binary reader reports the first character that is neither <c>0</c> nor <c>1</c>, and where.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="position">The index of the first such character.</param>
    [TestMethod]
    [DataRow("2111110", 0)]
    [DataRow("011 110", 3)]
    [DataRow("011111x", 6)]
    public void TryParseBinary_WhenACharacterIsNotABinaryDigit_ShouldReportItsPosition(string text, int position)
    {
        Assert.AreEqual(CalendarValueSet.ParseFailure.Character, CalendarValueSet.TryParseBinary(text, 7, out ulong bits, out int actual));
        Assert.AreEqual(position, actual);
        Assert.AreEqual(0UL, bits);
    }

    /// <summary>
    /// Verifies that only a text of the domain's width made of <c>0</c> and <c>1</c> has the binary form's shape.
    /// </summary>
    /// <param name="text">The text to test.</param>
    /// <param name="expected">Whether the text has the binary form's shape.</param>
    [TestMethod]
    [DataRow("010101010101", true)]
    [DataRow("000000000000", true)]
    [DataRow("01010101010", false)]
    [DataRow("0101010101010", false)]
    [DataRow("0101 0101010", false)]
    [DataRow("1,10", false)]
    public void IsBinary_WhenTested_ShouldRequireTheWidthInBinaryDigits(string text, bool expected)
    {
        Assert.AreEqual(expected, CalendarValueSet.IsBinary(text, 12));
    }
}
