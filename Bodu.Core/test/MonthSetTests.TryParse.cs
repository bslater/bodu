// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSetTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public sealed partial class MonthSetTests
{
    /// <summary>
    /// Verifies that <see cref="MonthSet.TryParse(string, out MonthSet)" /> detects each letter mask and answers
    /// <see langword="true" /> with the set it describes.
    /// </summary>
    /// <param name="kat">The expected set and its mask.</param>
    [TestMethod]
    [DynamicData(
        nameof(LetterMaskData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenTextIsALetterMask_ShouldReturnTrueAndReadIt(ValidKat<MonthSet, string> kat)
    {
        bool parsed = MonthSet.TryParse(kat.Expected, out MonthSet result);

        Assert.IsTrue(parsed);
        Assert.AreEqual(kat.Input, result);
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.TryParse(string, out MonthSet)" /> answers <see langword="false" /> with the
    /// empty set for text that is neither the binary form, a letter mask nor a list.
    /// </summary>
    /// <param name="text">The text.</param>
    [TestMethod]
    [DataRow("JFMAMJJASONX")]
    [DataRow("J_-_________")]
    [DataRow("___________J")]
    [DataRow(" JFM________D")]
    [DataRow("JFM_______D")]
    public void TryParse_WhenTextIsNotALetterMaskOrAList_ShouldReturnFalseAndEmptySet(string text)
    {
        bool parsed = MonthSet.TryParse(text, out MonthSet result);

        Assert.IsFalse(parsed);
        Assert.AreEqual(MonthSet.Empty, result);
    }
}
