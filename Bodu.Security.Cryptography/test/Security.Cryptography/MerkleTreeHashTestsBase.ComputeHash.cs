// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTestsBase.ComputeHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

public abstract partial class MerkleTreeHashTestsBase<THasher>
{
    /// <summary>Invokes <c>ComputeHash(byte[])</c> on the hasher.</summary>
    protected abstract byte[] ComputeHash(THasher hasher, byte[] data);

    /// <summary>Invokes <c>ComputeHash(byte[], int, int)</c> on the hasher.</summary>
    protected abstract byte[] ComputeHash(THasher hasher, byte[] data, int offset, int count);

    /// <summary>Invokes <c>ComputeHash(ReadOnlySpan&lt;byte&gt;)</c> on the hasher.</summary>
    protected abstract byte[] ComputeHash(THasher hasher, ReadOnlySpan<byte> data);

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Factory robustness — gaps that neither original suite covered
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a factory that throws propagates its exception out of <c>ComputeHash</c> —
    /// confirming the implementation does not silently swallow factory errors and does not hang
    /// when no algorithm instance can be created.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The exception may surface in one of three shapes depending on the implementation:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>The raw <see cref="InvalidOperationException" />, for synchronous implementations that propagate unchanged.</description></item>
    ///   <item><description>An <see cref="AggregateException" /> wrapping it, when the factory runs on parallel worker tasks.</description></item>
    ///   <item><description>An outer exception whose <see cref="Exception.InnerException" /> is the original, if the implementation wraps it.</description></item>
    /// </list>
    /// <para>
    /// The test uses a plain <c>try</c>/<c>catch</c> rather than
    /// <see cref="Assert.ThrowsExactly{T}(Action)" /> because newer MSTest versions require an
    /// exact type match on <c>T</c> — which would reject every derived type even when <c>T</c>
    /// is <see cref="Exception" />. A generous <see cref="TimeoutAttribute" /> guards against a
    /// regression where the implementation hangs awaiting workers whose factory call has failed.
    /// </para>
    /// </remarks>
    [TestMethod]
    [Timeout(10_000)]
    public void ComputeHash_WhenFactoryThrows_ShouldPropagateFactoryException()
    {
        Func<HashAlgorithm> faultyFactory = () => throw new InvalidOperationException("boom");

        using THasher hasher = Construct(faultyFactory, 4, 2);

        Exception? caught = null;
        try
        {
            ComputeHash(hasher, MakeData(8));
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.IsNotNull(caught,
            "ComputeHash did not throw — the factory's InvalidOperationException was silently swallowed.");

        var propagated =
            caught is InvalidOperationException
            || (caught is AggregateException agg
                && agg.Flatten().InnerExceptions.Any(e => e is InvalidOperationException))
            || caught.InnerException is InvalidOperationException;

        Assert.IsTrue(propagated,
            $"Expected the factory's InvalidOperationException to propagate; got " +
            $"{caught.GetType().Name}: {caught.Message}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — argument validation
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a negative offset raises <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetIsNegative_ShouldThrowExactly()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ComputeHash(hasher, MakeData(4), -1, 4);
        });
    }

    /// <summary>
    /// Verifies that a negative count raises <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCountIsNegative_ShouldThrowExactly()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ComputeHash(hasher, MakeData(4), 0, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>offset + count</c> exceeding the array length raises
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetPlusCountExceedsArrayLength_ShouldThrowExactly()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ComputeHash(hasher, MakeData(4), 2, 4);
        });
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> byte array to the <c>ComputeHash(byte[])</c>
    /// overload raises <see cref="ArgumentNullException" /> rather than silently treating the missing
    /// array as empty input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenByteArrayIsNull_ShouldThrowExactly()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ComputeHash(hasher, (byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> byte array to the
    /// <c>ComputeHash(byte[], int, int)</c> overload raises <see cref="ArgumentNullException" />
    /// rather than a <see cref="ReadOnlySpan{T}" />-constructor exception keyed to the wrong parameter.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenByteArrayIsNullWithOffsetAndCount_ShouldThrowExactly()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ComputeHash(hasher, (byte[])null!, 0, 0);
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — result shape
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the hash returned by the span overload is non-null and has the expected
    /// length for the additive monitoring algorithm (4 bytes).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSpanProvided_ShouldReturnHashOfExpectedLength()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        var result = ComputeHash(hasher, MakeData(8).AsSpan());

        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.Length); // MonitoringHashAlgorithm: sizeof(uint)
    }

    /// <summary>
    /// Verifies that all three byte-based ComputeHash overloads produce identical output for
    /// the same input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSameInputUsedAcrossOverloads_ShouldReturnIdenticalHashes()
    {
        var data = MakeData(13);

        using THasher h1 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using THasher h2 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using THasher h3 = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        var fromSpan = ComputeHash(h1, data.AsSpan());
        var fromArray = ComputeHash(h2, data);
        var fromSlice = ComputeHash(h3, data, 0, data.Length);

        CollectionAssert.AreEqual(fromSpan, fromArray, "Span vs array mismatch.");
        CollectionAssert.AreEqual(fromSpan, fromSlice, "Span vs slice mismatch.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — determinism
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that hashing the same input on two separate instances yields identical results.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSameInputHashedOnTwoInstances_ShouldReturnIdenticalResults()
    {
        var data = MakeData(17);

        using THasher h1 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using THasher h2 = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        CollectionAssert.AreEqual(ComputeHash(h1, data), ComputeHash(h2, data));
    }

    /// <summary>
    /// Verifies that two inputs differing by a single byte produce different hashes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputsDifferByOneByte_ShouldReturnDifferentHashes()
    {
        var a = MakeData(8);
        var b = (byte[])a.Clone();
        b[3] ^= 0xFF;

        using THasher h1 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using THasher h2 = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        CollectionAssert.AreNotEqual(ComputeHash(h1, a), ComputeHash(h2, b));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — slice semantics
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the (offset, count) overload hashes only the specified region and produces
    /// the same result as the span overload over that region.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOffsetAndCountSpecified_ShouldHashOnlyThatRegion()
    {
        var data = MakeData(12);
        const int offset = 2;
        const int count = 6;

        using THasher h1 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using THasher h2 = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        CollectionAssert.AreEqual(
            ComputeHash(h1, data, offset, count),
            ComputeHash(h2, data.AsSpan(offset, count)));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — input immutability (gap in original suites)
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that mutating the input array after <c>ComputeHash</c> returns does not retroactively
    /// change the computed hash — the implementation must not retain a reference to the caller's buffer.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputMutatedAfterReturn_ShouldNotAffectReturnedHash()
    {
        var data = MakeData(9);
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        var result = ComputeHash(hasher, data);

        // Scramble the caller's buffer. The returned hash must not change.
        Array.Fill(data, (byte)0xFF);

        CollectionAssert.AreEqual(expected, result,
            "ComputeHash returned a buffer whose contents depend on the caller's mutable array.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — various block/fanOut configurations
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the computed hash matches the hand-computed additive root across a range
    /// of block sizes, fan-out values, and input lengths.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2, 7)]
    [DataRow(4, 2, 8)]
    [DataRow(4, 2, 9)]
    [DataRow(4, 3, 12)]
    [DataRow(4, 4, 16)]
    [DataRow(8, 2, 25)]
    [DataRow(16, 3, 50)]
    public void ComputeHash_WhenVariousConfigurationsUsed_ShouldMatchHandComputedRoot(
        int blockSize, int fanOut, int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using THasher hasher = Construct(Factory, blockSize, fanOut);
        var actual = ComputeHash(hasher, data);

        CollectionAssert.AreEqual(expected, actual,
            $"Mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — hand-crafted vectors (documents the reduction algorithm exactly)
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vector A — single full block, fanOut=2.
    /// Data {1,2,3,4} → leaf sum=10 → root = { 0x0A, 0x00, 0x00, 0x00 }.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSingleFullBlock_ShouldReturnLeafHashAsRoot()
    {
        byte[] data = { 1, 2, 3, 4 };
        var expected = BitConverter.GetBytes((uint)10);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector B — two full blocks, fanOut=2.
    /// Data {1..8} → leaves 10,26 → root = sum({10,0,0,0,26,0,0,0}) = 36 → { 0x24, 0x00, 0x00, 0x00 }.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenTwoFullBlocks_ShouldCombineIntoRoot()
    {
        byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8 };
        var expected = BitConverter.GetBytes((uint)36);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector C — one full + one partial block, fanOut=2.
    /// Data {1..5} → leaves 10, 5 (padded) → root = 15.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenPartialTailBlock_ShouldZeroPadAndProduceCorrectRoot()
    {
        byte[] data = { 1, 2, 3, 4, 5 };
        var expected = BitConverter.GetBytes((uint)15);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector D — three full blocks, fanOut=2 (uneven: 3 leaves → 2 internal nodes).
    /// Data {1..12} → leaves 10,26,42 → level1 groups produce 36 and 42 → root 78.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenThreeLeavesWithFanOutTwo_ShouldProduceCorrectTwoLevelRoot()
    {
        byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var expected = BitConverter.GetBytes((uint)78);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector E — three full blocks, fanOut=3 (all three fit in one group).
    /// Data {1..12} → leaves 10,26,42 → root 78 in a single reduction step.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenThreeLeavesWithFanOutThree_ShouldReduceInOneStep()
    {
        byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var expected = BitConverter.GetBytes((uint)78);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 3);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector F — blockSize=1, every byte is its own leaf.
    /// Data {10,20,30,40} → root 100 → { 0x64, 0x00, 0x00, 0x00 }.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenBlockSizeIsOne_ShouldTreatEachByteAsLeaf()
    {
        byte[] data = { 10, 20, 30, 40 };
        var expected = BitConverter.GetBytes((uint)100);

        using THasher hasher = Construct(Factory, blockSize: 1, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector G — single byte, partial block only.
    /// Data {7} → padded {7,0,0,0} → root 7.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSingleByteInput_ShouldZeroPadAndReturnLeafAsRoot()
    {
        byte[] data = { 7 };
        var expected = BitConverter.GetBytes((uint)7);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Vector H — two full + one partial block, fanOut=3.
    /// Data {1..9} → leaves 10,26,9 → root 45.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenNineBytesWithFanOutThree_ShouldReduceAllThreeLeavesInOneStep()
    {
        byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var expected = BitConverter.GetBytes((uint)45);

        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 3);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — boundary coverage
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies inputs around a single-block boundary: blockSize-1, blockSize, blockSize+1.
    /// </summary>
    [TestMethod]
    [DataRow(3)]  // partial only
    [DataRow(4)]  // exactly one block
    [DataRow(5)]  // one full + one partial
    public void ComputeHash_AtSingleBlockBoundary_ShouldProduceCorrectRoot(int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data),
            $"Boundary length {dataLength} produced wrong root.");
    }

    /// <summary>
    /// Verifies inputs around a full fan-out group: fanOut*blockSize ± 1.
    /// </summary>
    [TestMethod]
    [DataRow(7)]   // two leaves, second partial
    [DataRow(8)]   // two full leaves → one internal root
    [DataRow(9)]   // three leaves, third partial
    public void ComputeHash_AtFanOutGroupBoundary_ShouldProduceCorrectRoot(int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data),
            $"Fan-out group boundary length {dataLength} produced wrong root.");
    }

    /// <summary>
    /// Exercises prime-length inputs to verify remainder handling at every tree level when
    /// groups never divide evenly.
    /// </summary>
    [TestMethod]
    [DataRow(7)]
    [DataRow(11)]
    [DataRow(13)]
    [DataRow(17)]
    [DataRow(23)]
    [DataRow(97)]
    public void ComputeHash_WhenInputLengthIsPrime_ShouldProduceCorrectRoot(int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data),
            $"Prime-length input {dataLength} produced wrong root.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — tree-shape edge cases
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a fan-out exceeding the leaf count still collapses into a correct root.
    /// </summary>
    [TestMethod]
    [DataRow(3, 10)]   // 3 leaves collapse in a single step with fanOut=10
    [DataRow(1, 100)]  // 1 leaf becomes the root directly
    public void ComputeHash_WhenFanOutExceedsLeafCount_ShouldProduceCorrectRoot(int leafCount, int fanOut)
    {
        var data = MakeData(DefaultBlockSize * leafCount);
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, fanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, fanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data),
            $"fanOut={fanOut} > leafCount={leafCount} produced wrong root.");
    }

    /// <summary>
    /// Verifies correctness for very wide fan-out (32) with many leaves (100).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenFanOutIsVeryLarge_ShouldProduceCorrectRoot()
    {
        const int blockSize = 4;
        const int fanOut = 32;
        var data = MakeData(blockSize * 100);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using THasher hasher = Construct(Factory, blockSize, fanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Verifies that a very deep tree (fanOut=2, 64 leaves, 6 levels) is handled without
    /// stack issues or level-transition bugs.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenTreeIsDeep_ShouldProduceCorrectRoot()
    {
        // blockSize=4 × 64 leaves = 256 bytes; fanOut=2 produces a 6-level tree
        // (64 → 32 → 16 → 8 → 4 → 2 → 1 root).
        const int blockSize = 4;
        const int fanOut = 2;
        var data = MakeData(blockSize * 64);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using THasher hasher = Construct(Factory, blockSize, fanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data),
            "Deep tree (6 levels) produced wrong root.");
    }

    /// <summary>
    /// Verifies that all-zero input produces the correct (non-zero) root rather than being
    /// short-circuited by any "empty" fast path.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsAllZeros_ShouldProduceCorrectRoot()
    {
        var data = new byte[DefaultBlockSize * 3];
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Verifies that all-0xFF input produces the correct root, exercising maximum byte-sum
    /// accumulation.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsAllMaxBytes_ShouldProduceCorrectRoot()
    {
        var data = Enumerable.Repeat((byte)0xFF, DefaultBlockSize * 3).ToArray();
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // ComputeHash — reuse & state isolation
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a second call on the same instance resets state and produces a result
    /// identical to a freshly constructed instance on the same input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledTwiceOnSameInstance_ShouldResetStateAndProduceSameResult()
    {
        var data = MakeData(9);

        using THasher reused = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using THasher fresh = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        var first = ComputeHash(reused, data);
        var second = ComputeHash(reused, data);
        var reference = ComputeHash(fresh, data);

        CollectionAssert.AreEqual(reference, first, "First call deviates from reference.");
        CollectionAssert.AreEqual(reference, second, "Second call deviates from reference.");
    }

    /// <summary>
    /// Verifies that switching between different input sizes across multiple calls does not
    /// contaminate state.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledWithAlternatingInputSizes_ShouldNotContaminateState()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        var small = MakeData(3);
        var large = MakeData(20);

        var expectedSmall = ComputeAdditiveRoot(small, DefaultBlockSize, DefaultFanOut);
        var expectedLarge = ComputeAdditiveRoot(large, DefaultBlockSize, DefaultFanOut);

        var hashSmall1 = ComputeHash(hasher, small);
        var hashLarge = ComputeHash(hasher, large);
        var hashSmall2 = ComputeHash(hasher, small);

        CollectionAssert.AreEqual(expectedSmall, hashSmall1, "First small call incorrect.");
        CollectionAssert.AreEqual(expectedLarge, hashLarge, "Large call incorrect.");
        CollectionAssert.AreEqual(expectedSmall, hashSmall2, "Second small call contaminated.");
    }

    /// <summary>
    /// Verifies that a partial tail-block from one call does not bleed into the buffer seen
    /// by the next call.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenPartialBlockFollowedByDifferentInput_ShouldNotReuseOldTailBytes()
    {
        using THasher hasher = Construct(Factory, blockSize: 4, fanOut: 2);

        // First call leaves a partial tail of {5} in any pooled buffer.
        var first = ComputeHash(hasher, new byte[] { 1, 2, 3, 4, 5 });

        // Second call: same prefix, no tail. Must equal a one-leaf root.
        var second = ComputeHash(hasher, new byte[] { 1, 2, 3, 4 });

        var expectedFirst = ComputeAdditiveRoot(new byte[] { 1, 2, 3, 4, 5 }, 4, 2);
        var expectedSecond = ComputeAdditiveRoot(new byte[] { 1, 2, 3, 4 }, 4, 2);

        CollectionAssert.AreEqual(expectedFirst, first, "First result wrong.");
        CollectionAssert.AreEqual(expectedSecond, second, "Old tail bytes leaked into second call.");
        CollectionAssert.AreNotEqual(first, second, "Different inputs should produce different roots.");
    }

    /// <summary>
    /// Verifies that many sequential calls with the same input produce consistent results,
    /// confirming reset restores a fully clean state every time.
    /// </summary>
    [TestMethod]
    [DataRow(5)]
    [DataRow(10)]
    [DataRow(25)]
    public void ComputeHash_WhenCalledRepeatedly_ShouldProduceConsistentResults(int callCount)
    {
        var data = MakeData(13);
        var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        for (var i = 0; i < callCount; i++)
        {
            CollectionAssert.AreEqual(expected, ComputeHash(hasher, data),
                $"Result mismatch on call {i + 1} of {callCount}.");
        }
    }

    /// <summary>
    /// Verifies that cycling through a sequence of different input lengths produces the correct
    /// result for every call.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputLengthAlternatesBetweenCalls_ShouldProduceCorrectResultEachTime()
    {
        int[] lengths = { 4, 12, 5, 8, 9, 4 };

        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        foreach (var len in lengths)
        {
            var data = MakeData(len);
            var expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);
            var actual = ComputeHash(hasher, data);

            CollectionAssert.AreEqual(expected, actual,
                $"Call with length={len} produced wrong result.");
        }
    }

    /// <summary>
    /// Verifies that a long-input call followed by a short-input call does not leak bytes from
    /// the longer input into the shorter result.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenLongInputFollowedByShortInput_ShouldNotLeakPreviousData()
    {
        const int blockSize = 4;
        const int fanOut = 2;

        var longData = MakeData(9, seed: 0xAA);
        var shortData = MakeData(5, seed: 0x11);
        var expectedShort = ComputeAdditiveRoot(shortData, blockSize, fanOut);

        using THasher hasher = Construct(Factory, blockSize, fanOut);
        ComputeHash(hasher, longData);
        var actual = ComputeHash(hasher, shortData);

        CollectionAssert.AreEqual(expectedShort, actual,
            "Second call was contaminated by the first call's tail-block bytes.");
    }
}
