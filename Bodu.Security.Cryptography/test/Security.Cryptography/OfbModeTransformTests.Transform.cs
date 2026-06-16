// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class OfbModeTransformTests
{
    /// <summary>
    /// Verifies that OFB encryption computes <c>Cᵢ = Pᵢ ⊕ Oᵢ</c> with <c>Oᵢ = E(Oᵢ₋₁)</c> and <c>O₀ = IV</c>. Uses the
    /// monitoring XOR cipher with a non-zero mask so the keystream is distinct from the raw IV, and asserts that each
    /// ciphertext block equals the plaintext XOR'd with the expected keystream block.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldApplyOfbKeystream()
    {
        // Use a non-zero xorMask so E(IV) != IV, which keeps the keystream distinct from the input.
        // With xorMask = 0x5C: E(x) = x ⊕ 0x5C, E(E(x)) = x (involutive), so keystream oscillates:
        //   O_0 = IV ⊕ 0x5C, O_1 = IV.
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x5C);
        byte[] iv = Enumerable.Repeat((byte)0x20, ExpectedBlockSize).ToArray();
        OfbModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        // Block 1 keystream = E(IV) = IV ⊕ 0x5C.
        byte[] keystream1 = iv.Select(b => (byte)(b ^ 0x5C)).ToArray();

        // Block 2 keystream = E(keystream1) = keystream1 ⊕ 0x5C = IV.
        byte[] keystream2 = keystream1.Select(b => (byte)(b ^ 0x5C)).ToArray();

        byte[] expectedBlock1 = plaintext[..ExpectedBlockSize].Zip(keystream1, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] expectedBlock2 = plaintext[ExpectedBlockSize..].Zip(keystream2, (a, b) => (byte)(a ^ b)).ToArray();

        CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray(), "First OFB block did not match expected keystream output.");
        CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray(), "Second OFB block did not match expected keystream output.");
    }

    /// <summary>
    /// Verifies that in OFB mode encryption and decryption are the same operation — XORing with the keystream produced
    /// from the IV. Both directions should reproduce the input after a round trip.
    /// </summary>
    [TestMethod]
    public void Transform_EncryptAndDecrypt_ShouldBeSymmetric()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x5C);
        byte[] iv = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i + 1)).ToArray();

        OfbModeTransform encrypt = CreateTransform(cipher, (byte[])iv.Clone());
        OfbModeTransform decrypt = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i * 7)).ToArray();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] recovered = new byte[plaintext.Length];

        encrypt.Transform(plaintext, ciphertext, encrypt: true);
        decrypt.Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered, "OFB decryption did not recover the original plaintext.");
    }

    /// <summary>
    /// Verifies that encrypting in OFB mode does not mutate the caller-supplied initialisation vector.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateIv()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = Enumerable.Repeat((byte)0x3C, ExpectedBlockSize).ToArray();
        byte[] ivCopy = (byte[])iv.Clone();
        OfbModeTransform transform = CreateTransform(cipher, iv);

        byte[] plaintext = new byte[ExpectedBlockSize];
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(ivCopy, iv, "OFB must not mutate the caller-supplied IV array.");
    }

    /// <summary>
    /// Verifies that OFB uses the cipher's encrypt primitive on both encryption and decryption paths. Like CFB and CTR,
    /// OFB never invokes the block cipher's decrypt primitive.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldUseCipherEncryptPrimitive()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
        OfbModeTransform transform = CreateTransform(cipher, iv);

        byte[] input = new byte[ExpectedBlockSize * 2];
        byte[] output = new byte[input.Length];

        transform.Transform(input, output, encrypt: false);

        Assert.AreEqual(2, cipher.EncryptBlockCount, "OFB decryption must use the cipher's encrypt primitive for every block.");
        Assert.AreEqual(0, cipher.DecryptBlockCount, "OFB decryption must never call the cipher's decrypt primitive.");
    }

    /// <summary>
    /// Verifies that transforming a single full block in OFB mode produces <c>P ⊕ E(IV)</c>.
    /// </summary>
    [TestMethod]
    public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x5C);
        byte[] iv = Enumerable.Repeat((byte)0x77, ExpectedBlockSize).ToArray();
        OfbModeTransform transform = CreateTransform(cipher, (byte[])iv.Clone());

        byte[] plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
        byte[] output = new byte[ExpectedBlockSize];

        transform.Transform(plaintext, output, encrypt: true);

        // Keystream = E(IV) = IV ⊕ 0x5C; output = plaintext ⊕ keystream.
        byte[] keystream = iv.Select(b => (byte)(b ^ 0x5C)).ToArray();
        byte[] expected = plaintext.Zip(keystream, (a, b) => (byte)(a ^ b)).ToArray();

        CollectionAssert.AreEqual(expected, output);
    }
}
