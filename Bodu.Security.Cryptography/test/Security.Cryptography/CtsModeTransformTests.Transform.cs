// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtsModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CtsModeTransformTests
{
    /// <summary>
    /// Verifies that CTS throws <see cref="ArgumentException" /> when the input is shorter than
    /// one full block. CTS requires at least one complete block to produce output of the same
    /// length as the input — there is nothing to steal from.
    /// </summary>
    [TestMethod]
    public void Transform_WhenInputShorterThanOneBlock_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = new byte[ExpectedBlockSize];

        byte[] input = new byte[ExpectedBlockSize - 1]; // one byte short of a full block
        byte[] output = new byte[input.Length];

        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateTransform(cipher, iv).Transform(input, output, encrypt: true),
            "CTS must throw ArgumentException when input is shorter than one full block.");
    }

    /// <summary>
    /// Verifies that CTS throws <see cref="ArgumentException" /> for an empty input, since there
    /// are no bytes to steal and no output can be produced.
    /// </summary>
    [TestMethod]
    public void Transform_WhenInputIsEmpty_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = new byte[ExpectedBlockSize];

        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateTransform(cipher, iv).Transform([], [], encrypt: true),
            "CTS must throw ArgumentException for empty input.");
    }

    /// <summary>
    /// Verifies that CTS round-trips correctly for a non-block-aligned input
    /// (one full block + partial block), recovering the original plaintext.
    /// </summary>
    [TestMethod]
    public void Transform_WithPartialFinalBlock_ShouldRoundTripCorrectly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
        int length = ExpectedBlockSize + ExpectedBlockSize / 2; // 1.5 blocks

        byte[] plaintext = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
        byte[] ciphertext = new byte[length];
        byte[] recovered = new byte[length];

        CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered,
            "CTS must recover the original plaintext for a non-block-aligned input.");
    }

    /// <summary>
    /// Verifies that CTS output length equals input length (no padding is added).
    /// </summary>
    [TestMethod]
    public void Transform_WithPartialFinalBlock_OutputLengthShouldEqualInputLength()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = new byte[ExpectedBlockSize];
        int length = ExpectedBlockSize + 7;

        byte[] input = new byte[length];
        byte[] output = new byte[length];

        int written = CreateTransform(cipher, iv).Transform(input, output, encrypt: true);

        Assert.AreEqual(length, written,
            "CTS must write exactly input.Length bytes — no padding.");
    }

    /// <summary>
    /// Verifies that CTS with a block-aligned input produces output identical to plain CBC.
    /// When no stealing is required, CTS degenerates to CBC.
    /// </summary>
    [TestMethod]
    public void Transform_WithBlockAlignedInput_ShouldMatchCbcOutput()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
        byte[] iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
        byte[] input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();

        byte[] ctsOutput = new byte[input.Length];
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(input, ctsOutput, encrypt: true);

        byte[] cbcOutput = new byte[input.Length];
        new CbcModeTransform(cipher, (byte[])iv.Clone()).Transform(input, cbcOutput, encrypt: true);

        CollectionAssert.AreEqual(cbcOutput, ctsOutput,
            "CTS with block-aligned input must produce the same output as CBC.");
    }

    /// <summary>
    /// Verifies that CTS with a non-aligned input produces different output than plain CBC,
    /// confirming that the steal operation actually changes the ciphertext arrangement.
    /// </summary>
    [TestMethod]
    public void Transform_WithNonAlignedInput_ShouldProduceDifferentOutputThanCbc()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = new byte[ExpectedBlockSize];
        int length = ExpectedBlockSize + 5;

        byte[] input = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
        byte[] ctsOutput = new byte[length];
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(input, ctsOutput, encrypt: true);

        // The CTS output must differ from simply taking the CBC output of the same bytes.
        byte[] cbcInput = input.Concat(new byte[ExpectedBlockSize - 5]).ToArray(); // padded
        byte[] cbcOutput = new byte[cbcInput.Length];
        new CbcModeTransform(cipher, (byte[])iv.Clone()).Transform(cbcInput, cbcOutput, encrypt: true);

        Assert.IsFalse(ctsOutput.SequenceEqual(cbcOutput[..length]),
            "CTS with non-aligned input must differ from padded-CBC to confirm the steal is applied.");
    }

    /// <summary>
    /// Verifies that CTS correctly round-trips a multi-block input with a partial final block
    /// (more than two full blocks + partial), exercising the body + CTS tail path.
    /// </summary>
    [TestMethod]
    public void Transform_WithMultipleFullBlocksPlusPartial_ShouldRoundTripCorrectly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xBB);
        byte[] iv = Enumerable.Repeat((byte)0x44, ExpectedBlockSize).ToArray();
        int length = ExpectedBlockSize * 4 + 9; // 4 full blocks + 9 bytes

        byte[] plaintext = Enumerable.Range(0, length).Select(i => (byte)(i * 3)).ToArray();
        byte[] ciphertext = new byte[length];
        byte[] recovered = new byte[length];

        CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered,
            "CTS must recover the original plaintext for a multi-block non-aligned input.");
    }

    /// <summary>
    /// Verifies that CTS does not mutate the caller-supplied IV.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateIv()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
        byte[] iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
        byte[] ivCopy = (byte[])iv.Clone();

        CreateTransform(cipher, iv)
            .Transform(new byte[ExpectedBlockSize + 3], new byte[ExpectedBlockSize + 3], encrypt: true);

        CollectionAssert.AreEqual(ivCopy, iv, "CTS must not mutate the caller-supplied IV.");
    }
}
