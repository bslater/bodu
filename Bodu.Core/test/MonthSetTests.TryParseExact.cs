// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSetTests.TryParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public sealed partial class MonthSetTests
{
    /// <summary>
    /// Verifies that <see cref="MonthSet.TryParseExact(string, string, out MonthSet)" /> with the <c>J</c> format
    /// answers <see langword="true" /> with the set each letter mask describes.
    /// </summary>
    /// <param name="kat">The expected set and its mask.</param>
    [TestMethod]
    [DynamicData(
        nameof(LetterMaskData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParseExact_WhenFormatIsJ_ShouldReturnTrueAndReadTheLetterMask(ValidKat<MonthSet, string> kat)
    {
        bool parsed = MonthSet.TryParseExact(kat.Expected, "J", out MonthSet result);

        Assert.IsTrue(parsed);
        Assert.AreEqual(kat.Input, result);
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.TryParseExact(string, string, out MonthSet)" /> answers
    /// <see langword="false" /> with the empty set for a mask whose placeholder is not the one the format names.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="other">A placeholder the format does not name.</param>
    [TestMethod]
    [DataRow("E", '_')]
    [DataRow("JU", '-')]
    [DataRow("D", '*')]
    [DataRow("JA", ' ')]
    public void TryParseExact_WhenMaskUsesAnotherPlaceholder_ShouldReturnFalseAndEmptySet(string format, char other)
    {
        bool parsed = MonthSet.TryParseExact("JFM" + new string(other, 8) + "D", format, out MonthSet result);

        Assert.IsFalse(parsed);
        Assert.AreEqual(MonthSet.Empty, result);
    }
}
