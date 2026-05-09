// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Test;
using Bodu.Test.IO;
using System.Security.Cryptography;
using static Bodu.Security.Cryptography.AsconHashA256Tests;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Returns test data that combines algorithm variants, named inputs, and expected hash results.
    /// </summary>
    /// <returns>A sequence of test case arguments: variant, input name, input bytes, and expected hash output.</returns>
    /// <remarks>
    /// This method is used to parameterize tests that verify the correctness of <see cref="HashAlgorithm.ComputeHash(byte[])" />
    /// against known input-output pairs.
    /// </remarks>
    public static IEnumerable<object[]> ComputeHashNamedInputTestData()
    {
        var instance = new TTest();
        foreach (var variant in instance.GetHashAlgorithmVariants())
        {
            var testVectors = instance.GetTestVectors(variant);
            foreach (var vector in testVectors)
            {
                yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput };
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> completes the current streaming
    /// operation when called after <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterTransformBlock_ShouldIncludePriorStreamingInput()
    {
        byte[] block1 = CryptoTestUtilities.ByteSequence256[..128];
        byte[] block2 = CryptoTestUtilities.ByteSequence256[128..256];

        byte[] combined = new byte[block1.Length + block2.Length];
        Buffer.BlockCopy(block1, 0, combined, 0, block1.Length);
        Buffer.BlockCopy(block2, 0, combined, block1.Length, block2.Length);

        using var expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(combined);

        using var algorithm = CreateAlgorithm();

        _ = algorithm.TransformBlock(block1, 0, block1.Length, null, 0);

        byte[] actual = algorithm.ComputeHash(block2, 0, block2.Length);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Hash" /> remains unavailable after
    /// <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> is called during an active streaming operation.
    /// </summary>
    [TestMethod]
    public void Hash_Get_WhenComputeHashCalledAfterTransformBlock_ShouldThrowCryptographicUnexpectedOperationException()
    {
        using var algorithm = CreateAlgorithm();

        _ = algorithm.TransformBlock(CryptoTestUtilities.ByteSequence256, 0, 128, null, 0);

        _ = algorithm.ComputeHash(CryptoTestUtilities.ByteSequence256, 128, 128);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            _ = algorithm.Hash;
        });
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="HashAlgorithm.ComputeHash(byte[])" /> with the same input produce the same result.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void ComputeHash_ShouldBeDeterministic(TVariant variant)
    {
        byte[] input = Enumerable.Range(0, 128).Select(i => (byte)(i % 256)).ToArray();
        using var algorithm1 = CreateAlgorithm(variant);
        using var algorithm2 = CreateAlgorithm(variant);
        byte[] hash1 = algorithm1.ComputeHash(input);
        byte[] hash2 = algorithm2.ComputeHash(input);

        CollectionAssert.AreEqual(hash1, hash2);
    }

    /// <summary>
    /// Verifies that hashing a zero-length region in a buffer matches the empty input hash using <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" />.
    /// </summary>
    [TestMethod]
    [DataRow(128, 128, 0)]
    [DataRow(128, 0, 0)]
    public void ComputeHash_ShouldReturnEmptyHash_WhenOffsetAndCountAreZero(int size, int offset, int count)
    {
        byte[] buffer = new byte[size];
        byte[] expected = ExpectedEmptyInputHash;
        using var algorithm1 = CreateAlgorithm();
        using var algorithm2 = CreateAlgorithm();

        byte[] actual = algorithm1.ComputeHash(buffer, offset, count); CollectionAssert.AreEqual(expected, actual);

        buffer[0] = 0xFF;
        buffer[^1] = 0xFF;
        actual = algorithm2.ComputeHash(buffer, offset, count);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> throws <see cref="ArgumentException" /> when the cound
    /// or segment is invalid.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0, -1)]
    [DataRow(0, 1, 0)]
    [DataRow(3, 0, 4)]
    [DataRow(3, 1, 3)]
    [DataRow(3, 3, 1)]
    public void ComputeHash_WhenOffsetAndCountCombinationIsInvalid_ShouldThrowExactly(int size, int offset, int count)
    {
        byte[] buffer = new byte[size];
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ComputeHash(buffer, offset, count);
        });
    }

    /// <summary>
    /// Verifies that hashing a segment with <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> produces the same result as the
    /// full input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetAndCountReferToSameData_ShouldProduceIdenticalHash()
    {
        byte[] bufferA = new byte[256];
        byte[] bufferB = new byte[257];
        CryptoHelpers.FillWithRandomNonZeroBytes(bufferA);
        Array.Copy(bufferA, 0, bufferB, 1, bufferA.Length);

        using var algorithm1 = CreateAlgorithm();
        using var algorithm2 = CreateAlgorithm();
        byte[] expected = algorithm1.ComputeHash(bufferA);
        byte[] actual = algorithm2.ComputeHash(bufferB, 1, bufferA.Length);
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// offset is negative.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetIsNegative_ShouldThrowExactly()
    {
        byte[] buffer = Array.Empty<byte>();
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.ComputeHash(buffer, -1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> produces consistent output for repeated identical input.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(64)]
    [DataRow(91)]
    [DataRow(1023)]
    [DataRow(1024)]
    public void ComputeHash_WhenReusedWithSameInput_ShouldProduceIdenticalHashResults(int size)
    {
        using var algorithm1 = CreateAlgorithm();
        using var algorithm2 = CreateAlgorithm();
        byte[] input = new byte[size];
        if (size > 0)
            CryptoHelpers.FillWithRandomNonZeroBytes(input);

        byte[] hashA = algorithm1.ComputeHash(input);
        byte[] hashB = algorithm2.ComputeHash(input);

        CollectionAssert.AreEqual(hashA, hashB);
    }

    /// <summary>
    /// Verifies that the synchronous <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" />
    /// path produces the correct hash value for incrementally growing inputs from empty input
    /// through one byte past the spec's coverage threshold.
    /// </summary>
    /// <param name="variant">The variant identifier supplied by the dynamic data source.</param>
    /// <returns>A task that completes when all incremental lengths have been verified.</returns>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public Task ComputeHash_WhenUsingIncrementalInput_ShouldMatchExpected(TVariant variant) =>
        AssertIncrementalInputAsync(variant,
            (algorithm, input, byteCount) => Task.FromResult(algorithm.ComputeHash(input, 0, byteCount)));

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash" />, when UsingNamedInput, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ComputeHashNamedInputTestData))]
    public void ComputeHash_WhenUsingNamedInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected)
    {
        var algorithm = CreateAlgorithm(variant);
        byte[] actual = algorithm.ComputeHash(input);

        TestHelpers.TraceWriteIfNotEqual(expected, actual);

        CollectionAssert.AreEqual(expected, actual, $"Hash mismatch for {testName} using variant '{variant}'.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash" />, with LargeInput, returns the expected value.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WithLargeInput_ShouldNotThrow()
    {
        using var algorithm = CreateAlgorithm();

        // Create data that will certainly exceed uint.MaxValue during computation
        byte[] data = Enumerable.Repeat((byte)255, 20_480_000).ToArray();

        // This will throw naturally if there's an overflow or other error
        var result = algorithm.ComputeHash(data);

        Assert.AreEqual(algorithm.HashSize / 8, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> throws <see cref="ArgumentNullException" /> when passed a null buffer.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WithNullBuffer_ShouldThrowExactly()
    {
        byte[] buffer = null!;
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.ComputeHash(buffer);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> throws <see cref="ArgumentNullException" /> when buffer
    /// is null.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WithNullBufferAndRange_ShouldThrowExactly()
    {
        byte[] buffer = null!;
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.ComputeHash(buffer, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(Stream)" /> throws <see cref="ArgumentNullException" /> when passed a null stream.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WithNullStream_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();

        // Expected behavior differs based on .NET version
#if NETFRAMEWORK || NETCOREAPP3_1

        // For .NET Framework and earlier .NET Core versions (up to 3.1), it throws ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(                () => {
        algorithm.ComputeHash((Stream)null!);
        });
#else

        // For .NET 5 and later, it throws NullReferenceException instead.
        Assert.ThrowsExactly<NullReferenceException>(() =>
        {
            algorithm.ComputeHash((Stream)null!);
        });
#endif
    }

    /// <summary>
    /// Verifies that computing a hash on a disposed <see cref="HashAlgorithm" /> instance throws
    /// <see cref="ObjectDisposedException" /> carrying the concrete <<typeparamref name="TAlgorithm"/> type
    /// name via <c>GetType().Name</c> (regression guard for an earlier <c>nameof(T)</c> bug on
    /// the base <see cref="HashAlgorithm" />).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenDisposed_ShouldThrowObjectDisposedExceptionWithAlgorithmName()
    {
        using var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        var ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.ComputeHash(new byte[] { 1, 2, 3 });
        });

        Assert.AreEqual(typeof(TAlgorithm).FullName, ex.ObjectName,
            $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
    }

    /// <summary>
    /// Verifies that <see cref="ComputeHash" /> always produces output whose length matches <see cref="HashAlgorithm.HashSize" />
    /// for a representative range of input lengths.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void ComputeHash_WhenCalled_ShouldAlwaysReturnCorrectHashSize(TVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);
        int expectedBytes = algorithm.HashSize / 8;

        foreach (int len in new[] { 0, 1, 4, 5, 12, 13, 24, 25, 100 })
        {
            byte[] hash = algorithm.ComputeHash(new byte[len]);
            Assert.AreEqual(
                expectedBytes,
                hash.Length,
                $"Expected {expectedBytes}-byte output (HashSize={algorithm.HashSize}) for input length {len}.");
        }
    }

    /// <summary>
    /// Verifies that representative input lengths produce distinct, non-trivial hash values, confirming that each
    /// internal algorithm path is exercised correctly and produces well-distributed output.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void ComputeHash_AtBoundaryLengths_ShouldProduceDistinctNonZeroHashes(TVariant variant)
    {
        var specification = GetSpecification(variant);

        var hashes = new List<byte[]>(specification.BoundaryLengths.Count);

        foreach (int len in specification.BoundaryLengths)
        {
            using var algorithm = CreateAlgorithm(variant);
            byte[] input = Enumerable.Range(1, len).Select(i => (byte)(i * 7)).ToArray();
            byte[] hash = algorithm.ComputeHash(input);

            Assert.IsTrue(
                hash.Any(b => b != 0),
                $"[{variant}] Length {len}: must not produce an all-zero hash for varied input.");

            hashes.Add(hash);
        }

        for (int i = 0; i < hashes.Count; i++)
        {
            for (int j = i + 1; j < hashes.Count; j++)
            {
                CollectionAssert.AreNotEqual(
                    hashes[i],
                    hashes[j],
                    $"[{variant}] Lengths {specification.BoundaryLengths[i]} and {specification.BoundaryLengths[j]} produced identical hashes.");
            }
        }
    }

    /// <summary>
    /// Verifies that a long, varied input exercises the iterative internal path and produces a hash with broad entropy
    /// spread across the output bytes. The minimum non-zero byte threshold is declared in the algorithm specification.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void ComputeHash_WhenInputIsLong_ShouldDistributeEntropyAcrossAllBytes(TVariant variant)
    {
        var specification = GetSpecification(variant);
        using var algorithm = CreateAlgorithm(variant);

        int outputBytes = specification.HashSize / 8;
        int threshold = specification.MinNonZeroBytesForLongInput ?? Math.Max(1, outputBytes / 2);

        byte[] input = Enumerable.Range(0, specification.LongInputLength)
            .Select(i => (byte)(i * 31 + 7))
            .ToArray();

        byte[] hash = algorithm.ComputeHash(input);

        Assert.AreEqual(
            outputBytes,
            hash.Length,
            $"[{variant}] Expected {outputBytes}-byte output for long input.");

        int nonZeroCount = hash.Count(b => b != 0);
        Assert.IsTrue(
            nonZeroCount >= threshold,
            $"[{variant}] Expected at least {threshold} of {outputBytes} output bytes to be non-zero; got {nonZeroCount}.");
    }

    /// <summary>
    /// Verifies that two inputs that span multiple input chunks produce different hashes.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void ComputeHash_ForMultiChunkInputs_ShouldProduceDistinctHashes(TVariant variant)
    {
        var specification = GetSpecification(variant);
        using var algorithm = CreateAlgorithm(variant);

        var bufferSize = specification.HashBlockSize * 2;
        byte[] inputA = TestHelpers.GenerateRandomNonZeroBytes(bufferSize);
        byte[] inputB = inputA.Copy()!;
        inputB[bufferSize - 2] = 0x00;

        byte[] hashA = algorithm.ComputeHash(inputA);
        byte[] hashB = algorithm.ComputeHash(inputB);

        CollectionAssert.AreNotEqual(hashA, hashB,
            "Distinct multi-chunk inputs must produce different hashes.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> completes the current streaming
    /// operation when called with empty input after multiple
    /// <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" /> calls.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledWithEmptyInputAfterMultipleTransformBlocks_ShouldIncludePriorStreamingInput()
    {
        byte[] block1 = CryptoTestUtilities.ByteSequence256[..128];
        byte[] block2 = CryptoTestUtilities.ByteSequence256[128..256];

        using var expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(CryptoTestUtilities.ByteSequence256);

        using var algorithm = CreateAlgorithm();

        _ = algorithm.TransformBlock(block1, 0, block1.Length, null, 0);
        _ = algorithm.TransformBlock(block2, 0, block2.Length, null, 0);

        byte[] actual = algorithm.ComputeHash(Array.Empty<byte>());

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> populates
    /// <see cref="HashAlgorithm.Hash" /> with the computed hash value.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalled_ShouldPopulateHashProperty()
    {
        using var algorithm = CreateAlgorithm();

        byte[] actual = algorithm.ComputeHash(CryptoTestUtilities.ByteSequence256);

        Assert.IsNotNull(algorithm.Hash);
        CollectionAssert.AreEqual(actual, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(Stream)" /> returns the expected empty-input hash
    /// when the supplied stream has no data.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamIsEmpty_ShouldReturnExpectedEmptyHash()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(Array.Empty<byte>());

        byte[] actual = algorithm.ComputeHash(stream);

        CollectionAssert.AreEqual(ExpectedEmptyInputHash, actual);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(Stream)" /> hashes from the stream's current position
    /// rather than from the beginning of the stream.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamPositionIsNonZero_ShouldHashFromCurrentPosition()
    {
        byte[] prefix = CryptoTestUtilities.ByteSequence256[..32];
        byte[] payload = CryptoTestUtilities.ByteSequence256[32..128];

        byte[] buffer = new byte[prefix.Length + payload.Length];
        Buffer.BlockCopy(prefix, 0, buffer, 0, prefix.Length);
        Buffer.BlockCopy(payload, 0, buffer, prefix.Length, payload.Length);

        using var expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(payload);

        using var algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(buffer);

        stream.Position = prefix.Length;

        byte[] actual = algorithm.ComputeHash(stream);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(Stream)" /> propagates an exception thrown by the
    /// source stream during a read operation.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamThrowsDuringRead_ShouldPropagateException()
    {
        using var algorithm = CreateAlgorithm();
        using var stream = new FaultingStream(new byte[0], 0);

        Assert.ThrowsExactly<IOException>(() =>
        {
            _ = algorithm.ComputeHash(stream);
        });
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same instance produce identical digests for identical input — guarding against
    /// state leakage across reuse. Skipped for one-shot algorithms that report
    /// <see cref="HashAlgorithm.CanReuseTransform" /> as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = CreateAlgorithm();
        if (!algorithm.CanReuseTransform)
        {
            Assert.Inconclusive($"{typeof(TAlgorithm).Name} reports CanReuseTransform=false; same-instance reuse is not supported.");
            return;
        }

        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }
}
