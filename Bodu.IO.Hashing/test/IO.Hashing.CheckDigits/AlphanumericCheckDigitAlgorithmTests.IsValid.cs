// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests.IsValid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;
public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that appending the algorithm's own computed check character to the body always yields a sequence
    /// that <c>IsValid</c> accepts.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check character.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void IsValid_WhenSequenceIncludesComputedCheckDigit_ShouldReturnTrue(string name, string body, char expectedCheck)
    {
        _ = name;
        string full = body + expectedCheck;
        Assert.IsTrue(IsValidStatic(full.AsSpan()), $"Expected '{full}' to be valid.");
    }

    /// <summary>
    /// Verifies that substituting the trailing check character for a different digit (or the sentinel
    /// <c>'X'</c> when applicable) makes <c>IsValid</c> reject the sequence.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The correct check character.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void IsValid_WhenCheckDigitIsTampered_ShouldReturnFalse(string name, string body, char expectedCheck)
    {
        _ = name;
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();

        for (char c = '0'; c <= '9'; c++)
        {
            if (c == expectedCheck) continue;
            string bad = body + c;
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of check digit).");
        }

        if (spec.OutputAlphabet == CheckDigitOutputAlphabet.DecimalDigitsOrX && expectedCheck != 'X')
        {
            string bad = body + 'X';
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of check digit to X).");
        }
    }
}
