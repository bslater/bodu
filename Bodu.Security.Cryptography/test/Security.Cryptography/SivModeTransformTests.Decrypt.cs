// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SivModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="SivModeTransform.Decrypt" /> zeroes the plaintext region of the output buffer before
    /// propagating an exception thrown by the CTR cipher mid-transform. The inherited fault sweep is vacuous for SIV
    /// because <see cref="CreateTransform" /> substitutes its own fixed-key AES ciphers, so this variant injects the
    /// fault directly into the CTR role.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCtrCipherFaultsMidTransform_ShouldZeroOutputBuffer()
    {
        RunSivFaultSweep(faultCtrCipher: true);
    }

    /// <summary>
    /// Verifies that <see cref="SivModeTransform.Decrypt" /> zeroes the plaintext region of the output buffer before
    /// propagating an exception thrown by the S2V (CMAC) cipher mid-transform. SIV is write-then-clear — the S2V
    /// recomputation runs over plaintext already written into the caller's buffer — so a fault inside S2V is the
    /// widest unverified-plaintext window this transform has.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenS2VCipherFaultsMidTransform_ShouldZeroOutputBuffer()
    {
        RunSivFaultSweep(faultCtrCipher: false);
    }

    /// <summary>
    /// Runs the mid-transform fault sweep against <see cref="SivModeTransform.Decrypt" />, injecting the fault into
    /// either the CTR cipher or the S2V cipher: counts the target cipher's block operations across a clean decrypt
    /// (construction included), then re-runs the decrypt once per call index with a fault at exactly that call and
    /// asserts the sentinel-filled output comes back all-zero whenever the fault escapes.
    /// </summary>
    /// <param name="faultCtrCipher">
    /// <see langword="true" /> to inject the fault into the CTR cipher; <see langword="false" /> for the S2V cipher.
    /// </param>
    private static void RunSivFaultSweep(bool faultCtrCipher)
    {
        byte[] iv = new byte[16];
        byte[] plaintext = new byte[32];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        var encTransform = new SivModeTransform(
            new AesBlockCipherFixture(s_s2vTestKey), new AesBlockCipherFixture(s_ctrTestKey), (byte[])iv.Clone());
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        // Count the target cipher's block operations across a clean decrypt (construction included).
        var countingCipher = new FaultingBlockCipher(
            new AesBlockCipherFixture(faultCtrCipher ? s_ctrTestKey : s_s2vTestKey), faultAfterCalls: 0);
        var countingTransform = new SivModeTransform(
            s2vCipher: faultCtrCipher ? new AesBlockCipherFixture(s_s2vTestKey) : countingCipher,
            ctrCipher: faultCtrCipher ? countingCipher : new AesBlockCipherFixture(s_ctrTestKey),
            iv: (byte[])iv.Clone());
        byte[] recovered = new byte[plaintext.Length];
        countingTransform.Decrypt(buf, recovered);
        CollectionAssert.AreEqual(plaintext, recovered,
            "Clean decrypt through the counting cipher must recover the original plaintext.");
        int totalCalls = countingCipher.CallCount;
        Assert.IsTrue(totalCalls > 0, "The decrypt path must exercise the target cipher.");

        for (int faultAt = 1; faultAt <= totalCalls; faultAt++)
        {
            var faultingCipher = new FaultingBlockCipher(
                new AesBlockCipherFixture(faultCtrCipher ? s_ctrTestKey : s_s2vTestKey), faultAt);

            SivModeTransform decTransform;
            try
            {
                decTransform = new SivModeTransform(
                    s2vCipher: faultCtrCipher ? new AesBlockCipherFixture(s_s2vTestKey) : faultingCipher,
                    ctrCipher: faultCtrCipher ? faultingCipher : new AesBlockCipherFixture(s_ctrTestKey),
                    iv: (byte[])iv.Clone());
            }
            catch (InvalidOperationException)
            {
                continue; // the fault landed in construction-time key derivation — Decrypt never ran
            }

            byte[] output = new byte[plaintext.Length];
            Array.Fill(output, (byte)0xCC); // sentinel — any non-zero value

            try
            {
                decTransform.Decrypt(buf, output);
            }
            catch (InvalidOperationException)
            {
                CollectionAssert.AreEqual(
                    new byte[plaintext.Length], output,
                    $"SivModeTransform must zero the output buffer when the {(faultCtrCipher ? "CTR" : "S2V")} " +
                    $"cipher faults on call {faultAt} of {totalCalls} so no plaintext or keystream bytes leak.");
            }
        }
    }
}
