// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests{T,T}.Compute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that the static <c>Compute</c> helper agrees with the streaming algorithm for every
    /// known-answer vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check code (unused; cross-comparison is between Compute and Append).</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(CheckDigitKatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(CheckDigitKatDisplayName))]
    public void Compute_WhenInvokedForKnownAnswer_ShouldAgreeWithStreamingAppend(string name, string body, string expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(algorithm.GetCurrentCheckDigits(), ComputeStatic(body.AsSpan()));
    }
    /// <summary>
    /// Verifies that the static <c>Compute</c> helper returns the expected check code for every known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check code.</param>
    [TestMethod]

    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(CheckDigitKatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(CheckDigitKatDisplayName))]
    public void Compute_WhenKnownAnswer_ShouldReturnExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        Assert.AreEqual(expectedCheck, ComputeStatic(body.AsSpan()));
    }

}
