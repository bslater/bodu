// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests.GetCurrentCheckDigit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that streaming the body one character at a time and reading <c>GetCurrentCheckDigit</c> matches
    /// the static <c>Compute</c> result at every prefix length.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The full body characters.</param>
    /// <param name="expectedCheck">The expected check character (unused).</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void GetCurrentCheckDigit_AtEveryPrefixLength_ShouldAgreeWithStaticCompute(string name, string body, char expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        for (var i = 0; i < body.Length; i++)
        {
            algorithm.Append(body[i]);
            var streaming = algorithm.GetCurrentCheckDigit();
            var fromCompute = ComputeStatic(body.AsSpan(0, i + 1));
            Assert.AreEqual(fromCompute, streaming, $"Prefix length {i + 1} (\"{body[..(i + 1)]}\").");
        }
    }
    /// <summary>
    /// Verifies that a freshly constructed algorithm reports the empty-body check character declared in the
    /// specification, when one is declared.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigit_WhenJustConstructed_ShouldReturnEmptyCheckDigit()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigit is not char expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that reading the current check character twice in succession — with no intervening appends —
    /// yields the same value, proving the getter is non-destructive.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigit_WhenReadRepeatedly_ShouldReturnSameValue()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("123".AsSpan());

        var first = algorithm.GetCurrentCheckDigit();
        var second = algorithm.GetCurrentCheckDigit();
        var third = algorithm.GetCurrentCheckDigit();

        Assert.AreEqual(first, second);
        Assert.AreEqual(second, third);
    }

}
