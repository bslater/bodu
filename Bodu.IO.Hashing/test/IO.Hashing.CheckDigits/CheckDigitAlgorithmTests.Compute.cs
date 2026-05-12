// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.Compute.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the static <c>Compute</c> helper returns the expected check digit for every known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body digits.</param>
    /// <param name="expectedCheck">The expected check digit.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData))]
    public void Compute_WhenKnownAnswer_ShouldReturnExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        Assert.AreEqual(expectedCheck, ComputeStatic(body.AsSpan()));
    }

    /// <summary>
    /// Verifies that the static <c>Compute</c> helper agrees with the streaming algorithm for every known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body digits.</param>
    /// <param name="expectedCheck">The expected check digit (unused; cross-comparison is between Compute and Append).</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData))]
    public void Compute_WhenInvokedForKnownAnswer_ShouldAgreeWithStreamingAppend(string name, string body, char expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(algorithm.GetCurrentCheckDigit(), ComputeStatic(body.AsSpan()));
    }

    /// <summary>
    /// Verifies that <c>Compute</c> on an empty span returns the empty-body check digit declared in the
    /// specification.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyIsEmpty_ShouldReturnEmptyCheckDigit()
    {
        CheckDigitAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.EmptyCheckDigit, ComputeStatic([]));
    }

    /// <summary>
    /// Verifies that <c>Compute</c> rejects non-digit characters with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsNonDigit_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ComputeStatic("12a45".AsSpan());
        });
    }
}
