// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;
public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <c>Reset</c> after an Append restores the algorithm to the empty-body check character
    /// declared in the specification (when one is declared).
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldRestoreEmptyCheckDigit()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigit is not char expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("123".AsSpan());
        algorithm.Reset();

        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that Reset between two append runs discards the first run's accumulated state so that only the
    /// second run contributes to the final check character.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append in the second run.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit for the second run.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Reset_BetweenAppends_ShouldDiscardPriorState(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append("987".AsSpan());
        algorithm.Reset();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }
}
