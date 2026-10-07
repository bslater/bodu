// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSetTests.ParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public sealed partial class MonthSetTests
{
    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> with the <c>J</c> format reads each letter mask,
    /// its initials and the format in either case.
    /// </summary>
    /// <param name="kat">The expected set and its mask.</param>
    [TestMethod]
    [DynamicData(
        nameof(LetterMaskData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ParseExact_WhenFormatIsJ_ShouldReadTheLetterMask(ValidKat<MonthSet, string> kat)
    {
        Assert.AreEqual(kat.Input, MonthSet.ParseExact(kat.Expected, "J"));
        Assert.AreEqual(kat.Input, MonthSet.ParseExact(kat.Expected.ToLowerInvariant(), "j"));
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> with the <c>J</c> format reads a mask whose
    /// placeholder is any one of <c>_</c>, <c>-</c>, <c>*</c> or a space, used throughout.
    /// </summary>
    /// <param name="placeholder">The placeholder the mask uses.</param>
    [TestMethod]
    [DataRow('_')]
    [DataRow('-')]
    [DataRow('*')]
    [DataRow(' ')]
    public void ParseExact_WhenFormatIsJ_ShouldReadAnyOnePlaceholder(char placeholder)
    {
        foreach (ValidKat<MonthSet, string> kat in LetterMaskCases)
            Assert.AreEqual(kat.Input, MonthSet.ParseExact(kat.Expected.Replace('_', placeholder), "J"), kat.Name);
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> with a format that names a placeholder, alone
    /// or after <c>J</c>, reads a mask that uses that placeholder.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="placeholder">The placeholder the format names.</param>
    [TestMethod]
    [DataRow("E", ' ')]
    [DataRow("U", '_')]
    [DataRow("D", '-')]
    [DataRow("A", '*')]
    [DataRow("JE", ' ')]
    [DataRow("JU", '_')]
    [DataRow("JD", '-')]
    [DataRow("ja", '*')]
    public void ParseExact_WhenFormatNamesAPlaceholder_ShouldReadThatPlaceholder(string format, char placeholder)
    {
        foreach (ValidKat<MonthSet, string> kat in LetterMaskCases)
            Assert.AreEqual(kat.Input, MonthSet.ParseExact(kat.Expected.Replace('_', placeholder), format), kat.Name);
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> with a format that names a placeholder throws
    /// <see cref="FormatException" /> for a mask that uses another.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="other">A placeholder the format does not name.</param>
    [TestMethod]
    [DataRow("E", '_')]
    [DataRow("U", '-')]
    [DataRow("JD", '*')]
    [DataRow("JA", ' ')]
    public void ParseExact_WhenMaskUsesAnotherPlaceholder_ShouldThrowFormatException(string format, char other)
    {
        string text = "JFM" + new string(other, 8) + "D";

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.ParseExact(text, format);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> throws <see cref="FormatException" /> for a
    /// mask that mixes two placeholders, with a message that quotes the first character that breaks it.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenMaskMixesPlaceholders_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.ParseExact("J_-_________", "J");
        });

        StringAssert.Contains(ex.Message, "'-'");
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> throws <see cref="FormatException" /> for an
    /// initial out of its month's place, with a message that quotes it and gives its one-based position.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenAnInitialIsOutOfPlace_ShouldThrowFormatException()
    {
        // J is the initial of January, June and July, but not of December, whose place it takes.
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.ParseExact("___________J", "J");
        });

        StringAssert.Contains(ex.Message, "'J'");
        StringAssert.Contains(ex.Message, "12");
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> throws <see cref="FormatException" /> for a mask
    /// that is not 12 characters, with a message that gives the length it requires.
    /// </summary>
    /// <param name="text">The mask.</param>
    [TestMethod]
    [DataRow("JFM_______D")]
    [DataRow("JFM_________D")]
    [DataRow(" JFM________D")]
    [DataRow("")]
    public void ParseExact_WhenMaskIsNotTwelveCharacters_ShouldThrowFormatException(string text)
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.ParseExact(text, "J");
        });

        StringAssert.Contains(ex.Message, "12");
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> reads a letter mask only under a letter-mask
    /// format: the <c>J</c> format reads neither the binary form nor a list, and the <c>G</c> and <c>B</c> formats do
    /// not read a mask.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="format">A format the text is not in.</param>
    [TestMethod]
    [DataRow("111000000001", "J")]
    [DataRow("1-3,12", "J")]
    [DataRow("JFM________D", "G")]
    [DataRow("JFM________D", "L")]
    [DataRow("JFM________D", "B")]
    public void ParseExact_WhenTextIsInAnotherFormat_ShouldThrowFormatException(string text, string format)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.ParseExact(text, format);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(string, string)" /> reads back every set written in every
    /// letter-mask format, given that format in either case.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenGivenToStringOfAnySetInALetterMaskFormat_ShouldReturnSet()
    {
        string[] formats = ["J", "E", "U", "D", "A", "JE", "JU", "JD", "JA", "j", "e", "ju", "jA"];

        for (ulong bits = 0; bits <= 0xFFF; bits++)
        {
            MonthSet set = MonthSet.FromUInt64(bits);

            foreach (string format in formats)
            {
                string text = set.ToString(format);

                Assert.AreEqual(set, MonthSet.ParseExact(text, format), $"bits {bits:X3}, format '{format}', text '{text}'");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ParseExact(ReadOnlySpan{char}, ReadOnlySpan{char})" /> reads back all 4,096 sets
    /// written in every format.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenSpanIsToStringOfAnySetInAnyFormat_ShouldReturnSet()
    {
        for (ulong bits = 0; bits <= 0xFFF; bits++)
        {
            MonthSet set = MonthSet.FromUInt64(bits);

            foreach (string format in Formats)
            {
                string text = set.ToString(format);

                Assert.AreEqual(set, MonthSet.ParseExact(text.AsSpan(), format.AsSpan()), $"bits {bits:X3}, format '{format}', text '{text}'");
            }
        }
    }
}
