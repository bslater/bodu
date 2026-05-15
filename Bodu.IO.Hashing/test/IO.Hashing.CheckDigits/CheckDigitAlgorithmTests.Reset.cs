// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that <see cref="CheckDigitAlgorithm.Reset" /> after an Append restores the algorithm to the
    /// empty-body check digit declared in the specification.
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldRestoreEmptyCheckDigit()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        CheckDigitAlgorithmSpecification spec = GetSpecification();

        algorithm.Append("987654".AsSpan());
        algorithm.Reset();

        Assert.AreEqual(spec.EmptyCheckDigit, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that Reset between two append runs discards the first run's accumulated state so that only the
    /// second run contributes to the final check digit.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body digits to append in the second run.</param>
    /// <param name="expectedCheck">The check digit the algorithm is expected to emit for the second run.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData))]
    public void Reset_BetweenAppends_ShouldDiscardPriorState(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append("0987654321".AsSpan());
        algorithm.Reset();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

}
