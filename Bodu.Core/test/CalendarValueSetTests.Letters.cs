// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetTests.Letters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public sealed partial class CalendarValueSetTests
{
    /// <summary>The letters of the days of the week, Sunday first.</summary>
    private const string DayLetters = "SMTWTFS";

    /// <summary>The letters of the months, January first.</summary>
    private const string MonthLetters = "JFMAMJJASOND";

    /// <summary>
    /// Verifies that a letter mask writes each selected value's letter, and the placeholder for the others, starting at
    /// the first position asked for.
    /// </summary>
    /// <param name="bits">The bitmap.</param>
    /// <param name="letters">The letter of each value.</param>
    /// <param name="first">The index of the value written first.</param>
    /// <param name="placeholder">The placeholder.</param>
    /// <param name="expected">The expected mask.</param>
    [TestMethod]
    [DataRow(0x3EUL, DayLetters, 0, '_', "_MTWTF_")]
    [DataRow(0x3EUL, DayLetters, 1, '_', "MTWTF__")]
    [DataRow(0x41UL, DayLetters, 1, '-', "-----SS")]
    [DataRow(0x249UL, MonthLetters, 0, '_', "J__A__J__O__")]
    [DataRow(0xFFFUL, MonthLetters, 0, ' ', MonthLetters)]
    [DataRow(0UL, MonthLetters, 0, '*', "************")]
    public void FormatLetters_WhenWritten_ShouldStartAtTheFirstPosition(ulong bits, string letters, int first, char placeholder, string expected)
    {
        Assert.AreEqual(expected, CalendarValueSet.FormatLetters(bits, letters, first, placeholder));
    }

    /// <summary>
    /// Verifies that a letter mask reads back to the same bitmap at either first position and with each placeholder.
    /// </summary>
    [TestMethod]
    public void FormatLetters_WhenReadBack_ShouldGiveTheSameBits()
    {
        foreach (ulong sample in SampleBits())
        {
            foreach ((string letters, int first) in new[] { (DayLetters, 0), (DayLetters, 1), (MonthLetters, 0) })
            {
                ulong bits = sample & CalendarValueSet.CreateMask(letters.Length);
                foreach (char placeholder in "_-* ")
                {
                    string text = CalendarValueSet.FormatLetters(bits, letters, first, placeholder);

                    CalendarValueSet.ParseFailure failure = CalendarValueSet.TryParseLetters(text, letters, first, placeholder: null, out ulong parsed, out _);

                    Assert.AreEqual(CalendarValueSet.ParseFailure.None, failure, text);
                    Assert.AreEqual(bits, parsed, text);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that without a first position the reader takes it from the first letter that fits only one of the two
    /// orders, reading in either case.
    /// </summary>
    /// <param name="text">The mask.</param>
    /// <param name="expected">The bits the mask selects.</param>
    [TestMethod]
    [DataRow("_MTWTF_", 0x3EUL)]
    [DataRow("MTWTF__", 0x3EUL)]
    [DataRow("mtwtf--", 0x3EUL)]
    [DataRow("______S", 0x40UL)]
    [DataRow("_____SS", 0x41UL)]
    public void TryParseLetters_WhenTheFirstPositionIsDetected_ShouldReadTheMaskInThatOrder(string text, ulong expected)
    {
        Assert.AreEqual(CalendarValueSet.ParseFailure.None, CalendarValueSet.TryParseLetters(text, DayLetters, first: null, placeholder: null, out ulong bits, out _));
        Assert.AreEqual(expected, bits);
    }

    /// <summary>
    /// Verifies that the reader reports the first character that fits neither its value's letter nor the placeholder,
    /// a second placeholder included.
    /// </summary>
    /// <param name="text">The mask.</param>
    /// <param name="placeholder">The placeholder the caller fixes, or the null character to take it from the text.</param>
    /// <param name="position">The index of the first character that does not fit.</param>
    [TestMethod]
    [DataRow("J_-A__J__O__", '\0', 2)]
    [DataRow("J__A__J__O_x", '\0', 11)]
    [DataRow("F___________", '\0', 0)]
    [DataRow("J__A__J__O__", '-', 1)]
    public void TryParseLetters_WhenACharacterDoesNotFit_ShouldReportItsPosition(string text, char placeholder, int position)
    {
        char? fixedPlaceholder = placeholder == '\0' ? null : placeholder;

        Assert.AreEqual(
            CalendarValueSet.ParseFailure.Character,
            CalendarValueSet.TryParseLetters(text, MonthLetters, 0, fixedPlaceholder, out ulong bits, out int actual));
        Assert.AreEqual(position, actual);
        Assert.AreEqual(0UL, bits);
    }

    /// <summary>
    /// Verifies that the reader reports a text of another length as a length failure.
    /// </summary>
    [TestMethod]
    public void TryParseLetters_WhenTheLengthDiffers_ShouldReportLength()
    {
        Assert.AreEqual(CalendarValueSet.ParseFailure.Length, CalendarValueSet.TryParseLetters("J__A__J__O_", MonthLetters, 0, null, out _, out _));
        Assert.AreEqual(CalendarValueSet.ParseFailure.Length, CalendarValueSet.TryParseLetters("J__A__J__O___", MonthLetters, 0, null, out _, out _));
    }
}
