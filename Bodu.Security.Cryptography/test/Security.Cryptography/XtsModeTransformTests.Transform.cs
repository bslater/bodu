// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class XtsModeTransformTests
{
    /// <summary>
    /// Verifies that XTS encryption followed by decryption under the same IV recovers the original
    /// plaintext for all blocks.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncryptThenDecrypt_ShouldRecoverOriginalPlaintext()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
        XtsModeTransform encrypt = CreateTransform(cipher, (byte[])iv.Clone());
        XtsModeTransform decrypt = CreateTransform(cipher, (byte[])iv.Clone());
        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] recovered = new byte[plaintext.Length];

        encrypt.Transform(plaintext, ciphertext, encrypt: true);
        decrypt.Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered,
            "XTS decryption must recover the original plaintext.");
    }

    /// <summary>
    /// Verifies that XTS encryption uses only the cipher's encrypt primitive (E for tweak derivation
    /// and E for each plaintext block). For n blocks the total encrypt call count is n + 1 (one extra
    /// for T_0 = E(IV) in the constructor).
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldUseOnlyCipherEncryptPrimitive()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = new byte[ExpectedBlockSize];
        XtsModeTransform transform = CreateTransform(cipher, iv);
        // Constructor has already called Encrypt once for T_0.

        byte[] input = new byte[ExpectedBlockSize * 2];
        byte[] output = new byte[input.Length];

        transform.Transform(input, output, encrypt: true);

        // T_0 = tweakCipher.Encrypt(iv) - counted against tweakCipher, not dataCipher.
        // dataCipher encrypts once per data block: 2 blocks → 2 calls.
        Assert.AreEqual(2, cipher.EncryptBlockCount,
            "XTS encryption must call the dataCipher's encrypt primitive once per data block.");
        Assert.AreEqual(0, cipher.DecryptBlockCount,
            "XTS encryption must never call the dataCipher's decrypt primitive.");
    }

    /// <summary>
    /// Verifies that XTS decryption uses the cipher's decrypt primitive for each ciphertext block and
    /// only uses encrypt for the initial T_0 = E(IV) derivation in the constructor.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldUseCipherDecryptPrimitive()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = new byte[ExpectedBlockSize];
        XtsModeTransform transform = CreateTransform(cipher, iv);
        // Constructor: EncryptBlockCount = 1.

        byte[] input = new byte[ExpectedBlockSize * 2];
        byte[] output = new byte[input.Length];

        transform.Transform(input, output, encrypt: false);

        // T_0 = tweakCipher.Encrypt(iv) - not counted against cipher (dataCipher).
        Assert.AreEqual(0, cipher.EncryptBlockCount,
            "XTS decryption must not call the dataCipher's encrypt primitive (T_0 is on tweakCipher).");
        Assert.AreEqual(2, cipher.DecryptBlockCount,
            "XTS decryption must call the dataCipher's decrypt primitive once per ciphertext block.");
    }

    /// <summary>
    /// Verifies that encrypting does not mutate the caller-supplied initialisation vector.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateIv()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
        byte[] iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
        byte[] ivCopy = (byte[])iv.Clone();
        XtsModeTransform transform = CreateTransform(cipher, iv);

        byte[] plaintext = new byte[ExpectedBlockSize];
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(ivCopy, iv,
            "XTS must not mutate the caller-supplied IV array.");
    }

    /// <summary>
    /// Verifies that the tweak advances between blocks so that two identical plaintext blocks
    /// produce different ciphertext. Uses real AES rather than MonitoringBlockCipher because an
    /// XOR-based cipher satisfies E(P ⊕ T) ⊕ T = P ⊕ mask for all T, making the tweak cancel
    /// entirely and producing identical output regardless of tweak advancement.
    /// </summary>
    [TestMethod]
    public void Transform_WithTwoIdenticalPlaintextBlocks_ShouldProduceDifferentCiphertextBlocks()
    {
        using var dataCipher = new AesBlockCipherFixture(new byte[ExpectedBlockSize]);
        using var tweakCipher = new AesBlockCipherFixture(new byte[ExpectedBlockSize]);
        byte[] iv = Enumerable.Repeat((byte)0x01, ExpectedBlockSize).ToArray();
        var transform = new XtsModeTransform(dataCipher, tweakCipher, iv);

        byte[] plaintext = Enumerable.Repeat((byte)0x42, ExpectedBlockSize * 2).ToArray();
        byte[] ciphertext = new byte[plaintext.Length];

        transform.Transform(plaintext, ciphertext, encrypt: true);

        CollectionAssert.AreNotEqual(
            ciphertext[..ExpectedBlockSize],
            ciphertext[ExpectedBlockSize..],
            "XTS must produce different ciphertext for identical plaintext blocks due to tweak advancement.");
    }

    /// <summary>
    /// Verifies that a single-block XTS encryption produces C = E(P ⊕ T_0) ⊕ T_0 where
    /// T_0 = E(IV). With the identity cipher (xorMask = 0x00), E(x) = x, so:
    /// T_0 = IV; C = (P ⊕ IV) ⊕ IV = P. This verifies the formula reduces correctly.
    /// </summary>
    [TestMethod]
    public void Transform_WithSingleBlock_ShouldApplyXtsTweakFormula()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] iv = Enumerable.Repeat((byte)0x55, ExpectedBlockSize).ToArray();
        XtsModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        // With identity cipher: T_0 = E(IV) = IV = 0x55...
        // C = E(P ⊕ T_0) ⊕ T_0 = (P ⊕ IV) ⊕ IV = P = 0x22...
        byte[] expected = plaintext; // identity cipher XOR cancellation
        CollectionAssert.AreEqual(expected, output,
            "XTS with identity cipher must reduce to C = P (XOR cancellation of tweak).");
    }

    // ── Long data units ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that encrypting data units of one block to several 4 KiB runs, in a separate buffer and in place,
    /// matches a block-at-a-time IEEE 1619 reference on the platform's AES for AES-128 and AES-256.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncryptingLongDataUnit_ShouldMatchBlockAtATimeReference()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] dataKey = AesReference.RandomBytes(keyLength, keyLength);
            byte[] tweakKey = AesReference.RandomBytes(keyLength, keyLength + 1);
            byte[] tweak = AesReference.RandomBytes(16, keyLength + 2);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] plaintext = AesReference.RandomBytes(length, length + 3);
                byte[] expected = ReferenceTransform(dataKey, tweakKey, tweak, plaintext, encrypt: true);

                CollectionAssert.AreEqual(expected, TransformWithAes(dataKey, tweakKey, tweak, plaintext, encrypt: true, inPlace: false), $"AES-{keyLength * 8}, {length} bytes");
                CollectionAssert.AreEqual(expected, TransformWithAes(dataKey, tweakKey, tweak, plaintext, encrypt: true, inPlace: true), $"AES-{keyLength * 8}, {length} bytes, in place");
            }
        }
    }

    /// <summary>
    /// Verifies that decrypting data units of one block to several 4 KiB runs, in a separate buffer and in place,
    /// matches a block-at-a-time IEEE 1619 reference on the platform's AES for AES-128 and AES-256.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecryptingLongDataUnit_ShouldMatchBlockAtATimeReference()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] dataKey = AesReference.RandomBytes(keyLength, keyLength + 11);
            byte[] tweakKey = AesReference.RandomBytes(keyLength, keyLength + 12);
            byte[] tweak = AesReference.RandomBytes(16, keyLength + 13);

            foreach (int length in AesReference.AlignedLengths)
            {
                byte[] ciphertext = AesReference.RandomBytes(length, length + 14);
                byte[] expected = ReferenceTransform(dataKey, tweakKey, tweak, ciphertext, encrypt: false);

                CollectionAssert.AreEqual(expected, TransformWithAes(dataKey, tweakKey, tweak, ciphertext, encrypt: false, inPlace: false), $"AES-{keyLength * 8}, {length} bytes");
                CollectionAssert.AreEqual(expected, TransformWithAes(dataKey, tweakKey, tweak, ciphertext, encrypt: false, inPlace: true), $"AES-{keyLength * 8}, {length} bytes, in place");
            }
        }
    }

    /// <summary>
    /// Transforms one data unit with <see cref="XtsModeTransform" /> over <see cref="AesBlockCipher" />.
    /// </summary>
    /// <param name="dataKey">The data key <c>K1</c>.</param>
    /// <param name="tweakKey">The tweak key <c>K2</c>.</param>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <param name="input">The data unit.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; <see langword="false" /> to decrypt.</param>
    /// <param name="inPlace"><see langword="true" /> to transform the input buffer in place.</param>
    /// <returns>The transformed data unit.</returns>
    private static byte[] TransformWithAes(byte[] dataKey, byte[] tweakKey, byte[] tweak, byte[] input, bool encrypt, bool inPlace)
    {
        using var dataCipher = new AesBlockCipher(dataKey);
        using var tweakCipher = new AesBlockCipher(tweakKey);
        using var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        byte[] buffer = (byte[])input.Clone();
        byte[] output = inPlace ? buffer : new byte[input.Length];
        transform.Transform(buffer, output, encrypt);
        return output;
    }

    /// <summary>
    /// Transforms one data unit with XTS one block at a time: <c>T = E_K2(tweak)</c>, then each block becomes
    /// <c>E_K1(block ⊕ T) ⊕ T</c> - or the decryption - and <c>T</c> is multiplied by <c>α</c>.
    /// </summary>
    /// <param name="dataKey">The data key <c>K1</c>.</param>
    /// <param name="tweakKey">The tweak key <c>K2</c>.</param>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <param name="input">The data unit.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; <see langword="false" /> to decrypt.</param>
    /// <returns>The transformed data unit.</returns>
    private static byte[] ReferenceTransform(byte[] dataKey, byte[] tweakKey, byte[] tweak, byte[] input, bool encrypt)
    {
        using System.Security.Cryptography.Aes data = AesReference.Create(dataKey);
        using System.Security.Cryptography.Aes tweaks = AesReference.Create(tweakKey);
        byte[] t = AesReference.Encrypt(tweaks, tweak);
        byte[] output = new byte[input.Length];
        for (int offset = 0; offset < input.Length; offset += 16)
        {
            byte[] block = AesReference.Xor(input[offset..(offset + 16)], t);
            block = encrypt ? AesReference.Encrypt(data, block) : AesReference.Decrypt(data, block);
            AesReference.Xor(block, t).CopyTo(output, offset);

            // Multiply by α in the little-endian representation, reducing by 0x87.
            bool carry = (t[15] & 0x80) != 0;
            for (int i = 15; i > 0; i--)
                t[i] = (byte)((t[i] << 1) | (t[i - 1] >> 7));

            t[0] = (byte)(t[0] << 1);
            if (carry)
                t[0] ^= 0x87;
        }

        return output;
    }
}
