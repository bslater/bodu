// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a reusable abstract base class for verifying the correctness of <see cref="Fletcher{T}" />
/// implementations (Fletcher-16, Fletcher-32, Fletcher-64).
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The Fletcher variant under test.</typeparam>
/// <remarks>
/// The shared behaviour captured here — empty input producing an all-zero checksum, the <c>Fletcher-</c> name
/// prefix, and sixteen-zero input stability — holds for every derived Fletcher variant. Size-specific known
/// answers are supplied by the concrete test classes through
/// <see cref="NonCryptographicHashAlgorithmSpecification.KnownAnswers" /> and
/// <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}.GetExpectedHashesForIncrementalInput(TVariant)" />.
/// </remarks>
public abstract partial class FletcherTests<TTest, TAlgorithm>
    : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : FletcherTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Fletcher<TAlgorithm>, new()
{


    /// <summary>
    /// Verifies that hashing an empty input with any Fletcher variant produces an all-zero checksum.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenInputIsEmpty_ShouldReturnAllZero()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        byte[] digest = algorithm.GetCurrentHash();

        Assert.IsTrue(digest.All(b => b == 0), $"Expected all-zero digest, was {Convert.ToHexString(digest)}.");
    }

    /// <summary>
    /// Verifies that hashing sixteen zero bytes with any Fletcher variant produces an all-zero checksum.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenInputIsSixteenZeros_ShouldReturnAllZero()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(new byte[16]);
        byte[] digest = algorithm.GetCurrentHash();

        Assert.IsTrue(digest.All(b => b == 0), $"Expected all-zero digest, was {Convert.ToHexString(digest)}.");
    }
    /// <inheritdoc />
    protected override TAlgorithm CreateAlgorithm(SingleTestVariant variant) => new();

}
