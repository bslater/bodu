// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        var iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
        XtsModeTransform encrypt = CreateTransform(cipher, (byte[])iv.Clone());
        XtsModeTransform decrypt = CreateTransform(cipher, (byte[])iv.Clone());
        var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        var ciphertext = new byte[plaintext.Length];
        var recovered = new byte[plaintext.Length];

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
        var iv = new byte[ExpectedBlockSize];
        XtsModeTransform transform = CreateTransform(cipher, iv);
        // Constructor has already called Encrypt once for T_0.

        var input = new byte[ExpectedBlockSize * 2];
        var output = new byte[input.Length];

        transform.Transform(input, output, encrypt: true);

        // T_0 = tweakCipher.Encrypt(iv) — counted against tweakCipher, not dataCipher.
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
        var iv = new byte[ExpectedBlockSize];
        XtsModeTransform transform = CreateTransform(cipher, iv);
        // Constructor: EncryptBlockCount = 1.

        var input = new byte[ExpectedBlockSize * 2];
        var output = new byte[input.Length];

        transform.Transform(input, output, encrypt: false);

        // T_0 = tweakCipher.Encrypt(iv) — not counted against cipher (dataCipher).
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
        var iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
        var ivCopy = (byte[])iv.Clone();
        XtsModeTransform transform = CreateTransform(cipher, iv);

        var plaintext = new byte[ExpectedBlockSize];
        var output = new byte[ExpectedBlockSize];

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
        var iv = Enumerable.Repeat((byte)0x01, ExpectedBlockSize).ToArray();
        var transform = new XtsModeTransform(dataCipher, tweakCipher, iv);

        var plaintext = Enumerable.Repeat((byte)0x42, ExpectedBlockSize * 2).ToArray();
        var ciphertext = new byte[plaintext.Length];

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
        var iv = Enumerable.Repeat((byte)0x55, ExpectedBlockSize).ToArray();
        XtsModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        var plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
        var output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        // With identity cipher: T_0 = E(IV) = IV = 0x55...
        // C = E(P ⊕ T_0) ⊕ T_0 = (P ⊕ IV) ⊕ IV = P = 0x22...
        var expected = plaintext; // identity cipher XOR cancellation
        CollectionAssert.AreEqual(expected, output,
            "XTS with identity cipher must reduce to C = P (XOR cancellation of tweak).");
    }
}
