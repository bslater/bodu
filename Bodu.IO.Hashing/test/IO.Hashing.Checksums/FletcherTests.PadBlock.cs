// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.PadBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains tests that exercise the protected <see cref="Fletcher{TSelf}.PadBlock" /> override directly.
/// Production Fletcher variants set <c>ShouldPadFinalBlock</c> to <see langword="false" />, so the override is
/// not reachable through the standard <c>Append</c>/<c>GetCurrentHash</c> pipeline; the test wraps it in a
/// derived type that exposes a public passthrough.
/// </summary>
[TestClass]
public sealed class FletcherPadBlockTests
{
    /// <summary>
    /// Verifies that <see cref="Fletcher{TSelf}.PadBlock" /> returns a buffer whose length matches the
    /// algorithm's block size and whose leading bytes are a copy of the supplied residual block.
    /// </summary>
    [TestMethod]
    public void PadBlock_WhenInvokedDirectly_ShouldReturnBlockSizedBufferContainingResidual()
    {
        TestFletcher16 algorithm = new();

        // Block size for Fletcher-16 is hashSize/16 = 1 byte, so feed a single-byte residual.
        byte[] padded = algorithm.PadBlockExposed(new byte[] { 0x42 }, messageLength: 0UL);

        Assert.IsNotNull(padded);
        Assert.AreEqual(1, padded.Length);
        Assert.AreEqual(0x42, padded[0]);
    }

    /// <summary>
    /// Verifies that <see cref="Fletcher{TSelf}.PadBlock" /> tolerates an empty residual on a Fletcher-32
    /// derivative, returning a zero-filled block-sized buffer.
    /// </summary>
    [TestMethod]
    public void PadBlock_WhenResidualIsEmptyOnFletcher32Variant_ShouldReturnZeroFilledBuffer()
    {
        TestFletcher32 algorithm = new();

        // Block size for Fletcher-32 is 2 bytes; an empty residual yields a {0x00, 0x00} buffer.
        byte[] padded = algorithm.PadBlockExposed(ReadOnlySpan<byte>.Empty, messageLength: 0UL);

        Assert.IsNotNull(padded);
        Assert.AreEqual(2, padded.Length);
        Assert.AreEqual(0x00, padded[0]);
        Assert.AreEqual(0x00, padded[1]);
    }

    /// <summary>
    /// Verifies that <see cref="Fletcher{TSelf}.PadBlock" /> on a Fletcher-64 derivative returns an
    /// 8-byte buffer that begins with the supplied residual and is zero-padded out to the block size.
    /// </summary>
    [TestMethod]
    public void PadBlock_WhenResidualIsPartialOnFletcher64Variant_ShouldZeroPadToBlockSize()
    {
        TestFletcher64 algorithm = new();

        byte[] padded = algorithm.PadBlockExposed(new byte[] { 0x10, 0x20, 0x30 }, messageLength: 99UL);

        Assert.IsNotNull(padded);
        Assert.AreEqual(8, padded.Length);
        Assert.AreEqual(0x10, padded[0]);
        Assert.AreEqual(0x20, padded[1]);
        Assert.AreEqual(0x30, padded[2]);
        for (int i = 3; i < padded.Length; i++)
            Assert.AreEqual(0x00, padded[i]);
    }

    /// <summary>
    /// A test-only Fletcher-16 derivative that exposes the protected
    /// <see cref="Fletcher{TSelf}.PadBlock" /> through a public passthrough.
    /// </summary>
    private sealed class TestFletcher16 : Fletcher<TestFletcher16>
    {
        public TestFletcher16()
            : base(16)
        {
        }

        public byte[] PadBlockExposed(ReadOnlySpan<byte> block, ulong messageLength) =>
            PadBlock(block, messageLength);
    }

    /// <summary>
    /// A test-only Fletcher-32 derivative that exposes the protected
    /// <see cref="Fletcher{TSelf}.PadBlock" /> through a public passthrough.
    /// </summary>
    private sealed class TestFletcher32 : Fletcher<TestFletcher32>
    {
        public TestFletcher32()
            : base(32)
        {
        }

        public byte[] PadBlockExposed(ReadOnlySpan<byte> block, ulong messageLength) =>
            PadBlock(block, messageLength);
    }

    /// <summary>
    /// A test-only Fletcher-64 derivative that exposes the protected
    /// <see cref="Fletcher{TSelf}.PadBlock" /> through a public passthrough.
    /// </summary>
    private sealed class TestFletcher64 : Fletcher<TestFletcher64>
    {
        public TestFletcher64()
            : base(64)
        {
        }

        public byte[] PadBlockExposed(ReadOnlySpan<byte> block, ulong messageLength) =>
            PadBlock(block, messageLength);
    }
}
