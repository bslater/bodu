// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmTests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialize" /> resets the internal accumulator so that
    /// a fresh hash computation starts from a clean state, regardless of whether the algorithm
    /// supports reuse.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void Initialize_AfterHashing_ShouldResetInternalState(TVariant variant)
    {
        var specification = GetSpecification(variant);
        using var algorithm = CreateAlgorithm(variant);
        int blockSize = specification.InputBlockSize;

        // Feed partial input — do NOT finalise — then reset
        byte[] input = Enumerable.Range(0, blockSize + (blockSize / 2))
                                 .Select(i => (byte)((i * 31) + 7))
                                 .ToArray();

        algorithm.TransformBlock(input, 0, input.Length, null, 0);
        algorithm.Initialize();

        // After Initialize, finalising with empty input should equal a clean empty-input hash
        algorithm.TransformFinalBlock([], 0, 0);

        using var reference = CreateAlgorithm(variant);
        reference.TransformFinalBlock([], 0, 0);

        CollectionAssert.AreEqual(
            reference.Hash,
            algorithm.Hash,
            $"[{variant}] Initialize did not fully reset internal accumulator — residual bytes leaked into the next computation.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialize" /> allows the algorithm to be reused,
    /// producing identical output for identical input across consecutive calls.
    /// Skipped for algorithms where <see cref="HashAlgorithmSpecification.CanReuseTransform" />
    /// is <see langword="false" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void Initialize_AfterHashing_ShouldClearResidualBlockState(TVariant variant)
    {
        var specification = GetSpecification(variant);

        if (!specification.CanReuseTransform)
        {
            Assert.Inconclusive(
                $"[{variant}] Algorithm does not support reuse after a completed transform; skipping residual state check.");
            return;
        }

        using var algorithm = CreateAlgorithm(variant);
        int blockSize = specification.InputBlockSize;
        byte[] input = Enumerable.Range(0, blockSize + (blockSize / 2))
                                 .Select(i => (byte)((i * 31) + 7))
                                 .ToArray();

        byte[] first = algorithm.ComputeHash(input);
        algorithm.Initialize();
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(
            first,
            second,
            $"[{variant}] Residual block state was not reset by Initialize — identical inputs produced different digests.");
    }
}
