// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSetTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public sealed partial class MonthSetTests
{
    /// <summary>
    /// Verifies that <see cref="MonthSet.Parse(string)" /> detects and reads each letter mask.
    /// </summary>
    /// <param name="kat">The expected set and its mask.</param>
    [TestMethod]
    [DynamicData(
        nameof(LetterMaskData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenTextIsALetterMask_ShouldReadIt(ValidKat<MonthSet, string> kat) =>
        Assert.AreEqual(kat.Input, MonthSet.Parse(kat.Expected));

    /// <summary>
    /// Verifies that <see cref="MonthSet.Parse(string)" /> reads a letter mask with any one placeholder throughout and
    /// its initials in either case.
    /// </summary>
    /// <param name="placeholder">The placeholder the mask uses.</param>
    [TestMethod]
    [DataRow('-')]
    [DataRow('*')]
    [DataRow(' ')]
    public void Parse_WhenALetterMaskUsesAnotherPlaceholder_ShouldReadIt(char placeholder)
    {
        foreach (ValidKat<MonthSet, string> kat in LetterMaskCases)
            Assert.AreEqual(kat.Input, MonthSet.Parse(kat.Expected.Replace('_', placeholder).ToLowerInvariant()), kat.Name);
    }

    /// <summary>
    /// Verifies that <see cref="MonthSet.Parse(string)" /> reports text that is neither the binary form, a letter mask
    /// nor a list as a malformed list, with the message that quotes the text, even when the text is twelve characters
    /// of initials and placeholders.
    /// </summary>
    /// <param name="text">The text.</param>
    [TestMethod]
    [DataRow("JFMAMJJASONX")]
    [DataRow("J_-_________")]
    [DataRow("___________J")]
    [DataRow(" JFM________D")]
    [DataRow("JFM________D ")]
    [DataRow("JFM_______D")]
    public void Parse_WhenTextIsNotALetterMaskOrAList_ShouldThrowFormatExceptionForAList(string text)
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = MonthSet.Parse(text);
        });

        StringAssert.Contains(ex.Message, $"'{text}'");
    }
}
