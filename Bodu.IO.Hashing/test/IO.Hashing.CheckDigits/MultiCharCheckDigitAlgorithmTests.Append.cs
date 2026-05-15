// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that appending an empty span leaves the running check code unchanged — equal to the value
    /// reported by a freshly constructed instance.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputIsEmpty_ShouldLeaveCheckDigitsUnchanged()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        TAlgorithm baseline = CreateAlgorithm();

        algorithm.Append([]);

        Assert.AreEqual(baseline.GetCurrentCheckDigits(), algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that appending a body in a single call produces the check code recorded for that known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenKnownAnswerIsAppendedInFull_ShouldProduceExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that appending a body one character at a time via the single-char overload produces the same
    /// check code as a single span-based call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenKnownAnswerIsAppendedOneCharAtATime_ShouldProduceExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        foreach (var ch in body)
            algorithm.Append(ch);

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that appending a body in two chunks produces the same check code as appending it in one call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenKnownAnswerIsSplitAcrossTwoChunks_ShouldProduceExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        if (body.Length < 2) return;

        TAlgorithm algorithm = CreateAlgorithm();
        var split = body.Length / 2;

        algorithm.Append(body.AsSpan(0, split));
        algorithm.Append(body.AsSpan(split));

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

}
