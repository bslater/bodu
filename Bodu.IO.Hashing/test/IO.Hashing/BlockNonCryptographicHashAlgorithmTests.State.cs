// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.State.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.Reset" /> clears the residual byte count
    /// alongside the cumulative total length.
    /// </summary>
    [TestMethod]
    public void Reset_WhenInvokedAfterPartialBlock_ShouldClearResidual()
    {
        StateInspectingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02 });
        Assert.AreEqual(2, hasher.ResidualByteCountExposed);

        hasher.Reset();

        Assert.AreEqual(0, hasher.ResidualByteCountExposed);
        Assert.AreEqual(0, hasher.ResidualBytesExposed.Length);
        Assert.AreEqual(0UL, hasher.TotalLengthExposed);
    }
    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualByteCount" /> tracks the number of
    /// bytes currently buffered but not yet processed.
    /// </summary>
    [TestMethod]
    public void ResidualByteCount_WhenInputIsAppended_ShouldReflectBufferedBytes()
    {
        StateInspectingBlockHasher hasher = new();

        Assert.AreEqual(0, hasher.ResidualByteCountExposed);

        hasher.Append(new byte[] { 0x01, 0x02 });
        Assert.AreEqual(2, hasher.ResidualByteCountExposed);

        // A block-completing append flushes the residual to ProcessBlock and clears the count.
        hasher.Append(new byte[] { 0x03, 0x04 });
        Assert.AreEqual(0, hasher.ResidualByteCountExposed);

        hasher.Append(new byte[] { 0x05 });
        Assert.AreEqual(1, hasher.ResidualByteCountExposed);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualBytes" /> exposes the buffered bytes
    /// in append order, and that the returned span length matches <c>ResidualByteCount</c>.
    /// </summary>
    [TestMethod]
    public void ResidualBytes_WhenInputIsBuffered_ShouldReturnBufferedBytesInOrder()
    {
        StateInspectingBlockHasher hasher = new();

        hasher.Append(new byte[] { 0xAA, 0xBB });

        byte[] copy = hasher.ResidualBytesExposed.ToArray();
        Assert.HasCount(2, copy);
        Assert.AreEqual(0xAA, copy[0]);
        Assert.AreEqual(0xBB, copy[1]);
    }

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.TotalLength" /> tracks the cumulative number
    /// of input bytes across multiple appends and is reset by <see cref="BlockNonCryptographicHashAlgorithm{T}.Reset()" />.
    /// </summary>
    [TestMethod]
    public void TotalLength_WhenInputIsAppended_ShouldReflectCumulativeBytes()
    {
        StateInspectingBlockHasher hasher = new();

        Assert.AreEqual(0UL, hasher.TotalLengthExposed);

        hasher.Append(new byte[] { 0x01, 0x02 });
        Assert.AreEqual(2UL, hasher.TotalLengthExposed);

        hasher.Append(new byte[] { 0x03, 0x04, 0x05 });
        Assert.AreEqual(5UL, hasher.TotalLengthExposed);

        hasher.Reset();
        Assert.AreEqual(0UL, hasher.TotalLengthExposed);
    }

    /// <summary>
    /// A test-only block hasher whose only job is to expose the protected base-class state properties
    /// (<see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualByteCount" />,
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.ResidualBytes" />,
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.TotalLength" />) to tests.
    /// </summary>
    private sealed class StateInspectingBlockHasher
        : BlockNonCryptographicHashAlgorithm<StateInspectingBlockHasher>
    {

        public StateInspectingBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        public int ResidualByteCountExposed => this.ResidualByteCount;

        public ReadOnlySpan<byte> ResidualBytesExposed => this.ResidualBytes;

        public ulong TotalLengthExposed => this.TotalLength;

        protected override StateInspectingBlockHasher Clone()
        {
            StateInspectingBlockHasher clone = new();
            clone.CopyResidualStateFrom(this);
            return clone;
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) =>
            block.ToArray();

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
        }

        protected override byte[] ProcessFinalBlock() => new byte[4];

        protected override bool ShouldPadFinalBlock() => false;

    }

}
