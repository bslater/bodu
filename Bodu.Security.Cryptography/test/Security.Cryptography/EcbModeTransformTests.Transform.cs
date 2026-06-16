// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class EcbModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="EcbModeTransform.Transform" />, when Decrypting, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldDecryptEachBlockIndependently()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA); // XOR cipher
        EcbModeTransform transform = CreateTransform(cipher, iv: null!); // ECB ignores IV

        byte[] original = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] encrypted = original.Select(b => (byte)(b ^ 0xAA)).ToArray();
        byte[] decrypted = new byte[encrypted.Length];

        transform.Transform(encrypted, decrypted, encrypt: false);

        CollectionAssert.AreEqual(original, decrypted, "Decrypted output should match original plaintext in ECB mode.");
    }

    /// <summary>
    /// Verifies that <see cref="EcbModeTransform.Transform" />, when Encrypting, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldEncryptEachBlockIndependently()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA); // Applies XOR with 0xAA per byte
        EcbModeTransform transform = CreateTransform(cipher, iv: null!); // ECB ignores IV

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        // ECB should apply the block transform directly to each block
        byte[] expectedBlock1 = plaintext[..ExpectedBlockSize].Select(b => (byte)(b ^ 0xAA)).ToArray();
        byte[] expectedBlock2 = plaintext[ExpectedBlockSize..].Select(b => (byte)(b ^ 0xAA)).ToArray();

        CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray(), "First block did not match expected ECB output.");
        CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray(), "Second block did not match expected ECB output.");
    }

    /// <summary>
    /// Verifies that <see cref="EcbModeTransform.Transform" />, when PlaintextBlocksAreIdentical, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WhenPlaintextBlocksAreIdentical_ShouldProduceIdenticalCipherTextBlocks()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        EcbModeTransform transform = CreateTransform(cipher, iv: null!);

        // Two identical plaintext blocks
        byte[] block = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();
        byte[] plaintext = block.Concat(block).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(output[..ExpectedBlockSize], output[ExpectedBlockSize..],
            "ECB mode should produce identical ciphertext for identical plaintext blocks.");
    }
}
