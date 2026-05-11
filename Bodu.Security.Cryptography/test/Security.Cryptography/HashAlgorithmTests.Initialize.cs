// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that reusing the algorithm instance after <see cref="HashAlgorithm.Initialise" /> resets the state correctly.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledBetweenHashes_ShouldResetStateForNewHash()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] input1 = CryptoTestUtilities.SimpleTextAsciiBytes;
        byte[] input2 = CryptoTestUtilities.ByteSequence256;

        byte[] hash1 = algorithm.ComputeHash(input1);

        algorithm.Initialize(); // reset state
        byte[] hash2 = algorithm.ComputeHash(input2);

        Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2), "Hashes should differ between independent inputs.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm" /> can be reused after finalizing a hash via
    /// <see cref="HashAlgorithm.TransformFinalBlock" /> and reinitializing, if supported.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterFinalBlock_ShouldAllowNewHashComputation()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] input = CryptoTestUtilities.ByteSequence256;
        algorithm.TransformFinalBlock(input, 0, input.Length);
        byte[] hash1 = algorithm.Hash!;

        if (!algorithm.CanReuseTransform)
        {
            // For one-shot algorithms, reinitialization without re-keying is invalid. Confirm that the second hash is not equal or that
            // key is cleared.
            algorithm.Initialize();

            // Either the key was cleared or a new random key was assigned; hash must differ.
            byte[] hash2 = algorithm.ComputeHash(input);
            CollectionAssert.AreNotEqual(hash1, hash2, "One-shot MAC should not yield the same result after reinitialization.");
        }
        else
        {
            algorithm.Initialize();

            byte[] hash2 = algorithm.ComputeHash(input);
            CollectionAssert.AreEqual(hash1, hash2, "Hashes should match after reinitializing with the same input.");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialise" /> mid-hashing resets any partially buffered data.
    /// </summary>
    [TestMethod]
    public void Initialize_MidHashing_ShouldResetPartialState()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] part1 = CryptoTestUtilities.SimpleTextAsciiBytes.Take(4).ToArray();
        byte[] part2 = CryptoTestUtilities.SimpleTextAsciiBytes.Skip(4).ToArray();

        algorithm.TransformBlock(part1, 0, part1.Length, null, 0);
        algorithm.Initialize(); // Reset mid-hash

        // Start a new hash
        byte[] result = algorithm.ComputeHash(part2);

        Assert.IsNotNull(result);

        // No expectations on the hashValue - just ensure no crash/corruption
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialise" /> after disposal throws an <see cref="ObjectDisposedException" />,
    /// matching base .NET behaviour.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenDisposed_ShouldThrowExactly()
    {
        // Arrange
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        // Act & Assert
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialize" /> discards input previously supplied through
    /// <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterTransformBlock_ShouldDiscardPriorInput()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        algorithm.TransformBlock(CryptoTestUtilities.ByteSequence256, 0, 128, null, 0);
        algorithm.Initialize();

        _ = algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(ExpectedEmptyInputHash, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialise" /> resets the internal accumulator so that
    /// a fresh hash computation starts from a clean state, regardless of whether the algorithm
    /// supports reuse.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void Initialize_AfterHashing_ShouldResetInternalState(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);
        using TAlgorithm algorithm = CreateAlgorithm(variant);
        int blockSize = specification.InputBlockSize;

        // Feed partial input — do NOT finalise — then reset
        byte[] input = Enumerable.Range(0, blockSize + (blockSize / 2))
                                 .Select(i => (byte)((i * 31) + 7))
                                 .ToArray();

        algorithm.TransformBlock(input, 0, input.Length, null, 0);
        algorithm.Initialize();

        // After Initialize, finalising with empty input should equal a clean empty-input hash
        algorithm.TransformFinalBlock([], 0, 0);

        using TAlgorithm reference = CreateAlgorithm(variant);
        reference.TransformFinalBlock([], 0, 0);

        CollectionAssert.AreEqual(
            reference.Hash,
            algorithm.Hash,
            $"[{variant}] Initialize did not fully reset internal accumulator — residual bytes leaked into the next computation.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialise" /> allows the algorithm to be reused,
    /// producing identical output for identical input across consecutive calls.
    /// Skipped for algorithms where <see cref="HashAlgorithmSpecification.CanReuseTransform" />
    /// is <see langword="false" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public virtual void Initialize_AfterHashing_ShouldClearResidualBlockState(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);

        if (!specification.CanReuseTransform)
        {
            Assert.Inconclusive(
                $"[{variant}] Algorithm does not support reuse after a completed transform; skipping residual state check.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithm(variant);
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

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed algorithm
    /// instance throws <see cref="ObjectDisposedException" /> rather
    /// than touching the cleared internal state.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);

        if (!specification.CanReuseTransform)
        {
            Assert.Inconclusive(
                $"[{variant}] Algorithm does not support reuse after a completed transform; skipping residual state check.");
            return;
        }
        using TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> before streaming input allows a normal
    /// hash operation to complete successfully.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledBeforeTransformBlock_ShouldAllowNormalHashing()
    {
        byte[] block1 = CryptoTestUtilities.ByteSequence256[..128];
        byte[] block2 = CryptoTestUtilities.ByteSequence256[128..256];

        byte[] combined = new byte[block1.Length + block2.Length];
        Buffer.BlockCopy(block1, 0, combined, 0, block1.Length);
        Buffer.BlockCopy(block2, 0, combined, block1.Length, block2.Length);

        using TAlgorithm expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(combined);

        using TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Initialize();

        _ = algorithm.TransformBlock(block1, 0, block1.Length, null, 0);
        _ = algorithm.TransformFinalBlock(block2, 0, block2.Length);

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> multiple times before hashing leaves the
    /// algorithm in a valid clean state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledTwice_ShouldAllowNormalHashing()
    {
        byte[] input = CryptoTestUtilities.ByteSequence256[..128];

        using TAlgorithm expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(input);

        using TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Initialize();
        algorithm.Initialize();

        _ = algorithm.TransformFinalBlock(input, 0, input.Length);

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }
}
