// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="MultiCharCheckDigitAlgorithm.Reset" /> after an Append restores the algorithm to
    /// the empty-body check code declared in the specification (when one is declared).
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldRestoreEmptyCheckDigits()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigits is not string expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("1234".AsSpan());
        algorithm.Reset();

        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that Reset between two append runs discards the first run's accumulated state so that only the
    /// second run contributes to the final check code.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append in the second run.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit for the second run.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Reset_BetweenAppends_ShouldDiscardPriorState(string name, string body, string expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append("123456".AsSpan());
        algorithm.Reset();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }
}
