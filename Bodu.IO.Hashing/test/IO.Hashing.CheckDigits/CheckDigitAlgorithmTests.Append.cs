// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that appending a body in a single call produces the check digit recorded for that known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body digits to append.</param>
    /// <param name="expectedCheck">The check digit the algorithm is expected to emit.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenKnownAnswerIsAppendedInFull_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body one character at a time via the single-char overload produces the same
    /// check digit as a single span-based call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body digits to append.</param>
    /// <param name="expectedCheck">The check digit the algorithm is expected to emit.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenKnownAnswerIsAppendedOneCharAtATime_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        foreach (char ch in body)
            algorithm.Append(ch);

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body in two chunks produces the same check digit as appending it in one call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body digits to append.</param>
    /// <param name="expectedCheck">The check digit the algorithm is expected to emit.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenKnownAnswerIsSplitAcrossTwoChunks_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        if (body.Length < 2) return;

        TAlgorithm algorithm = CreateAlgorithm();
        int split = body.Length / 2;

        algorithm.Append(body.AsSpan(0, split));
        algorithm.Append(body.AsSpan(split));

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending an empty span leaves the running check digit unchanged.
    /// </summary>
    [TestMethod]
    public void Append_WhenSpanIsEmpty_ShouldLeaveCheckDigitUnchanged()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        char initial = algorithm.GetCurrentCheckDigit();

        algorithm.Append(ReadOnlySpan<char>.Empty);

        Assert.AreEqual(initial, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that streaming a prefix of a known-answer body and reading <c>GetCurrentCheckDigit</c> matches
    /// the static <c>Compute</c> result for the same prefix — cross-checking streaming parity with the
    /// allocate-free path at intermediate body lengths.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The full body digits.</param>
    /// <param name="expectedCheck">The expected check digit for the full body (unused in this test).</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void GetCurrentCheckDigit_AtEveryPrefixLength_ShouldAgreeWithStaticCompute(string name, string body, char expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        for (int i = 0; i < body.Length; i++)
        {
            algorithm.Append(body[i]);
            char streaming = algorithm.GetCurrentCheckDigit();
            char fromCompute = ComputeStatic(body.AsSpan(0, i + 1));
            Assert.AreEqual(fromCompute, streaming, $"Prefix length {i + 1} (\"{body[..(i + 1)]}\").");
        }
    }
}
