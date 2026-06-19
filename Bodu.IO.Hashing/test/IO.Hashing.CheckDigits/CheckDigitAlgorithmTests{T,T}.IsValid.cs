// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests{T,T}.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that substituting the trailing check digit for a different digit makes <c>IsValid</c> reject
    /// the sequence.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body digits.</param>
    /// <param name="expectedCheck">The correct check digit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData))]
    public void IsValid_WhenCheckDigitIsTampered_ShouldReturnFalse(string name, string body, char expectedCheck)
    {
        _ = name;
        for (char c = '0'; c <= '9'; c++)
        {
            if (c == expectedCheck) continue;
            string bad = body + c;
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of check digit).");
        }
    }

    /// <summary>
    /// Verifies that <c>IsValid</c> returns <see langword="false" /> — without throwing — when the input
    /// contains a non-digit character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputContainsNonDigit_ShouldReturnFalse() => Assert.IsFalse(IsValidStatic("123a5".AsSpan()));

    /// <summary>
    /// Verifies that <c>IsValid</c> rejects the empty span. A full-sequence validator requires at least a check
    /// digit, so an empty input cannot satisfy the algorithm's invariant.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputIsEmpty_ShouldReturnFalse() => Assert.IsFalse(IsValidStatic([]));
    /// <summary>
    /// Verifies that appending the algorithm's own computed check digit to the body always yields a sequence
    /// that <c>IsValid</c> accepts.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body digits.</param>
    /// <param name="expectedCheck">The expected check digit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData))]
    public void IsValid_WhenSequenceIncludesComputedCheckDigit_ShouldReturnTrue(string name, string body, char expectedCheck)
    {
        _ = name;
        string full = body + expectedCheck;
        Assert.IsTrue(IsValidStatic(full.AsSpan()), $"Expected '{full}' to be valid.");
    }

}
