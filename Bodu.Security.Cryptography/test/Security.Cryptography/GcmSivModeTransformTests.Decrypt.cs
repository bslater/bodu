// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class GcmSivModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" /> zeroes the plaintext region of the output buffer
    /// before propagating an exception thrown by the <em>derived</em> per-nonce encryption cipher mid-transform. The
    /// inherited fault sweep only exercises the master cipher, whose calls all happen during construction-time key
    /// derivation; this variant injects the fault through the cipher factory so it lands inside the CTR decryption
    /// and tag recomputation that <see cref="GcmSivModeTransform.Decrypt" /> runs.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenDerivedCipherFaultsMidTransform_ShouldZeroOutputBuffer()
    {
        byte[] key = new byte[16];
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        using var encMaster = new AesBlockCipherFixture(key);
        var encTransform = new GcmSivModeTransform(encMaster, k => new AesBlockCipherFixture(k), (byte[])iv.Clone());
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        // Count the derived-cipher calls a clean decrypt makes (the factory runs exactly once, at construction).
        using var countingMaster = new AesBlockCipherFixture(key);
        FaultingBlockCipher? countingCipher = null;
        var countingTransform = new GcmSivModeTransform(
            countingMaster,
            k => countingCipher = new FaultingBlockCipher(new AesBlockCipherFixture(k), faultAfterCalls: 0),
            (byte[])iv.Clone());
        byte[] recovered = new byte[plaintext.Length];
        countingTransform.Decrypt(buf, recovered);
        CollectionAssert.AreEqual(plaintext, recovered,
            "Clean decrypt through the counting derived cipher must recover the original plaintext.");
        int totalCalls = countingCipher!.CallCount;
        Assert.IsTrue(totalCalls > 0, "The decrypt path must exercise the derived cipher.");

        for (int faultAt = 1; faultAt <= totalCalls; faultAt++)
        {
            using var master = new AesBlockCipherFixture(key);
            var decTransform = new GcmSivModeTransform(
                master,
                k => new FaultingBlockCipher(new AesBlockCipherFixture(k), faultAt),
                (byte[])iv.Clone());

            byte[] output = new byte[plaintext.Length];
            Array.Fill(output, (byte)0xCC); // sentinel - any non-zero value

            try
            {
                decTransform.Decrypt(buf, output);
            }
            catch (InvalidOperationException)
            {
                CollectionAssert.AreEqual(
                    new byte[plaintext.Length], output,
                    $"GcmSivModeTransform must zero the output buffer when the derived cipher faults " +
                    $"on call {faultAt} of {totalCalls} so no plaintext or keystream bytes leak.");
            }
        }
    }

    /// <summary>
    /// Verifies that ciphertext and tags the block-at-a-time reference sealed authenticate and decrypt to the original
    /// plaintext across the same lengths and alignments as the encryption cross-check.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenGivenReferenceSealedMessage_ShouldRecoverPlaintext()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = ReferenceBytes(keyLength, keyLength + 21);
            byte[] nonce = ReferenceBytes(12, keyLength + 22);

            foreach (int length in s_referenceLengths)
            {
                foreach (int aadLength in s_referenceAadLengths)
                {
                    byte[] plaintext = ReferenceBytes(length, length + 23);
                    byte[] aad = ReferenceBytes(aadLength, aadLength + 29);
                    byte[] sealedMessage = ReferenceSeal(key, nonce, aad, plaintext);

                    using var master = new AesBlockCipher(key);
                    using var transform = new GcmSivModeTransform(master, k => new AesBlockCipher(k), ReferenceIv(nonce));
                    transform.ProcessAssociatedData(aad);
                    byte[] recovered = new byte[length];
                    int written = transform.Decrypt(sealedMessage, recovered);

                    Assert.AreEqual(length, written);
                    CollectionAssert.AreEqual(plaintext, recovered, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }
}
