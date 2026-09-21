// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.StreamingMemory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Measures what the two block-mode entry points actually retain while a stream is being consumed, rather than
/// asserting the fold's shape and inferring the rest.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Rfc6962MerkleTree.ComputeRootOfBlocks(Stream, int, CancellationToken)" /> is documented as folding the
/// tree as it reads, holding one subtree per set bit of the leaf count — so its peak is
/// <c>O(blockSize + log n · HashLength)</c> and does not grow with the input.
/// <see cref="Rfc6962MerkleTree.ComputeBlocked(Stream, int, CancellationToken)" /> deliberately retains every leaf
/// hash instead, which is what makes a later authentication path possible. Those are memory claims, and the rest of
/// the suite only checks that both produce the same root.
/// </para>
/// <para>
/// The probe samples the live heap partway through the stream, with the algorithm's own state still reachable, and
/// compares the growth between a small and a large input. Absolute figures would be meaningless — the measurement
/// includes the whole test host — so every assertion is about the <em>difference</em> between two runs in the same
/// process, which cancels that baseline.
/// </para>
/// <para>
/// The leaf-retaining pass is measured too, and required to grow. A probe that cannot see growth where growth is
/// certain proves nothing about its absence elsewhere, so that half is what keeps this test from being vacuous.
/// </para>
/// <para>
/// Both thresholds are deliberately loose against the measured behavior. On a four-core x64 container the fold
/// grows 136-160 bytes going from 4,096 to 65,536 leaves, while the leaf-retaining pass grows 3,588,096 bytes, so
/// the ratio assertion clears its bound by roughly three orders of magnitude and the anti-vacuity floor by 3.6x.
/// The fold's figure is not merely small but close to the predicted one: the sample lands nine-tenths of the way
/// through each stream, where the fold holds one subtree per set bit of the blocks consumed so far, and the two
/// extra set bits between the two inputs account for most of it. Because every assertion compares two samples
/// taken in the same process, the spread across repeated runs is a few dozen bytes rather than a few megabytes.
/// </para>
/// </remarks>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>The block size used by the memory probe. Small, so a modest input yields many leaves.</summary>
    private const int ProbeBlockSize = 1024;

    /// <summary>The smaller leaf count of the growth comparison.</summary>
    private const int ProbeSmallLeafCount = 4096;

    /// <summary>The larger leaf count of the growth comparison — sixteen times the smaller.</summary>
    private const int ProbeLargeLeafCount = 65536;

    /// <summary>
    /// Verifies that the streaming fold's retained memory does not grow with the input: folding sixteen times as
    /// many leaves holds essentially the same amount live, while the leaf-retaining pass over the same inputs grows
    /// by roughly one hash per leaf.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ComputeRootOfBlocks_WhenFoldingALargerStream_ShouldNotRetainMoreThanTheSmallerOne()
    {
        var tree = new Rfc6962MerkleTree(SHA256.Create);

        long foldSmall = MeasureLiveBytesMidStream(ProbeSmallLeafCount, folding: true, tree);
        long foldLarge = MeasureLiveBytesMidStream(ProbeLargeLeafCount, folding: true, tree);
        long retainSmall = MeasureLiveBytesMidStream(ProbeSmallLeafCount, folding: false, tree);
        long retainLarge = MeasureLiveBytesMidStream(ProbeLargeLeafCount, folding: false, tree);

        long foldGrowth = foldLarge - foldSmall;
        long retainGrowth = retainLarge - retainSmall;

        // Sanity: the probe must be able to see growth where growth is certain. The leaf-retaining pass holds one
        // 32-byte hash plus its object header per leaf, so 61,440 extra leaves is well over a mebibyte.
        Assert.IsTrue(
            retainGrowth > 1_000_000,
            $"the probe failed to observe the leaf-retaining pass growing; measured {retainGrowth:N0} bytes");

        // The fold holds at most one subtree per set bit of the leaf count. Sixteen times the input adds at most a
        // handful of hashes, so anything approaching the retaining pass's growth means the fold is accumulating.
        Assert.IsTrue(
            foldGrowth < retainGrowth / 8,
            $"the fold grew {foldGrowth:N0} bytes against the retaining pass's {retainGrowth:N0}; " +
            "the streaming fold appears to accumulate with input size");
    }

    /// <summary>
    /// Runs one block-mode pass over a synthesized stream and returns the live heap sampled once the stream is
    /// nine-tenths consumed, with the algorithm's own state still reachable.
    /// </summary>
    /// <param name="leafCount">The number of <see cref="ProbeBlockSize" /> blocks to feed.</param>
    /// <param name="folding">
    /// <see langword="true" /> to drive the logarithmic fold; <see langword="false" /> to drive the pass that
    /// retains every leaf hash.
    /// </param>
    /// <param name="tree">The tree to compute with.</param>
    /// <returns>The live managed heap size, in bytes, at the sampling point.</returns>
    private static long MeasureLiveBytesMidStream(int leafCount, bool folding, Rfc6962MerkleTree tree)
    {
        long sampled = 0;
        long length = (long)leafCount * ProbeBlockSize;

        using var source = new SamplingStream(length, (long)(length * 0.9), () => sampled = LiveBytes());

        // The result is kept in a local the JIT cannot elide, so the pass is not optimized away.
        byte[] root = folding
            ? tree.ComputeRootOfBlocks(source, ProbeBlockSize)
            : tree.ComputeBlocked(source, ProbeBlockSize).Root;

        Assert.AreEqual(tree.HashLength, root.Length);
        Assert.AreNotEqual(0L, sampled, "the sampling point was never reached");

        return sampled;
    }

    /// <summary>
    /// Returns the live managed heap size after a settling full collection.
    /// </summary>
    /// <returns>The live managed heap size, in bytes.</returns>
    private static long LiveBytes()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    /// <summary>
    /// A read-only stream that synthesizes a fixed number of bytes without materializing them, and raises a callback
    /// once, the first time the read position crosses a chosen offset.
    /// </summary>
    /// <remarks>
    /// Generating rather than storing the payload is what keeps the probe honest: a backing buffer would dominate
    /// the very measurement being taken.
    /// </remarks>
    private sealed class SamplingStream : Stream
    {
        private readonly long _length;
        private readonly long _sampleAt;
        private readonly Action _onSample;
        private long _position;
        private bool _sampled;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingStream" /> class.
        /// </summary>
        /// <param name="length">The total number of bytes the stream yields.</param>
        /// <param name="sampleAt">The read position at which the callback is raised.</param>
        /// <param name="onSample">The callback raised once, when the position first reaches <paramref name="sampleAt" />.</param>
        public SamplingStream(long length, long sampleAt, Action onSample)
        {
            _length = length;
            _sampleAt = sampleAt;
            _onSample = onSample;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _length;

        /// <inheritdoc />
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            int take = (int)Math.Min(count, _length - _position);
            if (take <= 0) return 0;

            // A cheap position-derived pattern: constant bytes would let a future implementation collapse blocks.
            for (int i = 0; i < take; i++)
                buffer[offset + i] = unchecked((byte)(_position + i));

            _position += take;

            if (!_sampled && _position >= _sampleAt)
            {
                _sampled = true;
                _onSample();
            }

            return take;
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // Nothing to flush on a read-only synthesized stream.
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
