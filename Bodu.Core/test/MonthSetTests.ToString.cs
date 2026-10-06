// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSetTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public sealed partial class MonthSetTests
{
    /// <summary>
    /// Verifies that <see cref="MonthSet.ToString(string)" /> with the <c>J</c> format, in either case, writes each
    /// selected month's initial in its place, January first, and <c>_</c> for a month not selected.
    /// </summary>
    /// <param name="kat">The set and the mask expected for it.</param>
    [TestMethod]
    [DynamicData(
        nameof(LetterMaskData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenFormatIsJ_ShouldWriteTheLetterMask(ValidKat<MonthSet, string> kat)
    {
        Assert.AreEqual(kat.Expected, kat.Input.ToString("J"));
        Assert.AreEqual(kat.Expected, kat.Input.ToString("j"));
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ToString(string)" /> with a format that names a placeholder, alone or after
    /// <c>J</c>, writes that placeholder for every month not selected.
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
    [DataRow("JA", '*')]
    [DataRow("e", ' ')]
    [DataRow("jd", '-')]
    [DataRow("jA", '*')]
    public void ToString_WhenFormatNamesAPlaceholder_ShouldWriteItForEachMonthNotSelected(string format, char placeholder)
    {
        foreach (ValidKat<MonthSet, string> kat in LetterMaskCases)
            Assert.AreEqual(kat.Expected.Replace('_', placeholder), kat.Input.ToString(format), kat.Name);
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.ToString(string)" /> throws <see cref="FormatException" /> for a letter-mask
    /// format that is not <c>J</c> followed by a placeholder letter, with a message that quotes the format.
    /// </summary>
    /// <param name="format">The unsupported format.</param>
    [TestMethod]
    [DataRow("JJ")]
    [DataRow("JG")]
    [DataRow("EJ")]
    [DataRow("UE")]
    [DataRow("JEU")]
    [DataRow("J ")]
    public void ToString_WhenLetterMaskFormatIsMalformed_ShouldThrowFormatException(string format)
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.All.ToString(format);
        });

        StringAssert.Contains(ex.Message, $"'{format}'");
    }
}
