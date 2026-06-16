// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class CbcModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> in decrypt mode inverts the CBC chain, XOR-unwrapping each ciphertext block against the prior ciphertext to recover the original plaintext.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldApplyCBCUnchaining()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00); // Identity
        byte[] iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
        CbcModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        // Encrypted block1 = plaintext1 ^ IV, encrypted block2 = plaintext2 ^ encrypted block1
        byte[] plaintext1 = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
        byte[] plaintext2 = Enumerable.Repeat((byte)0x44, ExpectedBlockSize).ToArray();
        byte[] block1 = plaintext1.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] block2 = plaintext2.Zip(block1, (a, b) => (byte)(a ^ b)).ToArray();

        byte[] ciphertext = block1.Concat(block2).ToArray();
        byte[] output = new byte[ciphertext.Length];

        transform.Transform(ciphertext, output, encrypt: false);

        CollectionAssert.AreEqual(plaintext1, output[..ExpectedBlockSize].ToArray(), "Decryption of first block failed.");
        CollectionAssert.AreEqual(plaintext2, output[ExpectedBlockSize..].ToArray(), "Decryption of second block failed.");
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> in encrypt mode XORs each plaintext block with the prior ciphertext (or IV for block 0) before calling the cipher — the defining chaining property of CBC.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldApplyCBCChaining()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00); // Identity cipher
        byte[] iv = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i << 4)).ToArray();
        CbcModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        byte[] expectedBlock1 = plaintext[..ExpectedBlockSize].Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] expectedBlock2 = plaintext[ExpectedBlockSize..].Zip(expectedBlock1, (a, b) => (byte)(a ^ b)).ToArray();

        CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray());
        CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> in encrypt mode leaves the caller's IV buffer untouched (the transform copies the IV rather than mutating it in place).
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateIV()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0xAB, ExpectedBlockSize).ToArray();
        byte[] ivCopy = (byte[])iv.Clone();
        CbcModeTransform transform = CreateTransform(cipher, iv);

        byte[] plaintext = Enumerable.Repeat((byte)0xCD, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(ivCopy, iv, "IV must not be mutated after encryption.");
    }

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" />, with SingleBlock, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
        CbcModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        byte[] expected = plaintext.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
        CollectionAssert.AreEqual(expected, output);
    }
}
