// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.IsValid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
        for (var c = '0'; c <= '9'; c++)
        {
            if (c == expectedCheck) continue;
            var bad = body + c;
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of check digit).");
        }
    }

    /// <summary>
    /// Verifies that <c>IsValid</c> returns <see langword="false" /> — without throwing — when the input
    /// contains a non-digit character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputContainsNonDigit_ShouldReturnFalse()
    {
        Assert.IsFalse(IsValidStatic("123a5".AsSpan()));
    }

    /// <summary>
    /// Verifies that <c>IsValid</c> accepts the empty span as vacuously valid — the natural limit of the
    /// algorithm's identity when no digits have been absorbed.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputIsEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(IsValidStatic([]));
    }
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
        var full = body + expectedCheck;
        Assert.IsTrue(IsValidStatic(full.AsSpan()), $"Expected '{full}' to be valid.");
    }

}
