// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.OpenRfc8439.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305AeadCore.OpenRfc8439" /> recovers the RFC 8439 Section 2.8.2 plaintext from the
    /// reference ciphertext and tag.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenGivenRfc8439Vector_ShouldRecoverPlaintext()
    {
        var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

        byte[] ciphertextWithTag = new byte[s_ciphertext.Length + s_tag.Length];
        s_ciphertext.CopyTo(ciphertextWithTag, 0);
        s_tag.CopyTo(ciphertextWithTag, s_ciphertext.Length);

        byte[] output = new byte[s_ciphertext.Length];
        int written = Poly1305AeadCore.OpenRfc8439(engine, s_associatedData, ciphertextWithTag, output);

        Assert.AreEqual(s_plaintext.Length, written);
        CollectionAssert.AreEqual(s_plaintext, output);
    }

    /// <summary>
    /// Verifies that <see cref="Poly1305AeadCore.OpenRfc8439" /> throws <see cref="CryptographicException" /> when the
    /// authentication tag has been altered.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenTagIsTampered_ShouldThrowCryptographicException()
    {
        var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

        byte[] ciphertextWithTag = new byte[s_ciphertext.Length + s_tag.Length];
        s_ciphertext.CopyTo(ciphertextWithTag, 0);
        s_tag.CopyTo(ciphertextWithTag, s_ciphertext.Length);
        ciphertextWithTag[^1] ^= 0xff;

        byte[] output = new byte[s_ciphertext.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = Poly1305AeadCore.OpenRfc8439(engine, s_associatedData, ciphertextWithTag, output);
        });
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with an engine that produces keystream in bulk recovers every
    /// plaintext, of every length from empty to past the widest kernel's run, sealed one keystream block at a time.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenSealedOneBlockAtATime_ShouldRecoverPlaintext()
    {
        var random = new Random(0x5EA1_0002);

        foreach (int length in MessageLengths)
        {
            byte[] plaintext = new byte[length];
            random.NextBytes(plaintext);
            byte[] sealedMessage = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] recovered = new byte[length];
            using var single = new SingleBlockStreamCipher(new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0));
            using var bulk = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

            _ = Poly1305AeadCore.SealRfc8439(single, s_associatedData, plaintext, sealedMessage);
            _ = Poly1305AeadCore.OpenRfc8439(bulk, s_associatedData, sealedMessage, recovered);

            CollectionAssert.AreEqual(plaintext, recovered, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value recovers the RFC 8439 Section 2.8.2 plaintext.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenKeystreamIsAValue_ShouldRecoverPlaintext()
    {
        ChaCha20Core.Keystream keystream = default;
        keystream.Initialize(s_key, s_nonce, counter: 0);
        byte[] output = new byte[s_plaintext.Length];

        int written = Poly1305AeadCore.OpenRfc8439(ref keystream, s_associatedData, Rfc8439CiphertextWithTag(), output);

        Assert.AreEqual(s_plaintext.Length, written);
        CollectionAssert.AreEqual(s_plaintext, output);
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value throws <see cref="CryptographicException" /> for an altered tag
    /// before writing any plaintext.
    /// </summary>
    [TestMethod]
    public void OpenRfc8439_WhenKeystreamIsAValueAndTagIsTampered_ShouldThrowWithoutWritingOutput()
    {
        byte[] ciphertextWithTag = Rfc8439CiphertextWithTag();
        ciphertextWithTag[^1] ^= 0x01;
        byte[] output = new byte[s_plaintext.Length];
        Array.Fill(output, (byte)0xCC);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(s_key, s_nonce, counter: 0);
            _ = Poly1305AeadCore.OpenRfc8439(ref keystream, s_associatedData, ciphertextWithTag, output);
        });

        Assert.IsTrue(output.All(value => value == 0xCC), "The output was written before the tag was verified.");
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value throws <see cref="CryptographicException" /> for an altered tag
    /// before writing any plaintext, both for messages short enough to be decrypted in one pass through a buffer and for
    /// longer ones.
    /// </summary>
    /// <param name="length">The length of the message, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(64)]
    [DataRow(960)]
    [DataRow(961)]
    [DataRow(2000)]
    public void OpenRfc8439_WhenTagIsTampered_ForEitherPath_ShouldThrowWithoutWritingOutput(int length)
    {
        var random = new Random(0x5EA1_0008 + length);
        byte[] sealedMessage = new byte[length + Poly1305AeadCore.TagBytes];
        using (var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0))
            _ = Poly1305AeadCore.SealRfc8439(engine, s_associatedData, NextBytes(random, length), sealedMessage);

        sealedMessage[^1] ^= 0x01;
        byte[] output = new byte[length];
        Array.Fill(output, (byte)0xCC);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(s_key, s_nonce, counter: 0);
            _ = Poly1305AeadCore.OpenRfc8439(ref keystream, s_associatedData, sealedMessage, output);
        });

        Assert.IsTrue(output.All(value => value == 0xCC), "The output was written before the tag was verified.");
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing with the keystream's draws planned for each kernel recovers
    /// every plaintext sealed with a ChaCha20 engine, for every message length from empty to past the longest drawn in
    /// one pass and a longer one.
    /// </summary>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void OpenRfc8439_WhenPlannedForEachKernel_ShouldRecoverPlaintext(string kernel)
    {
        AssertPlannedMatchesTheEngine(nameof(Poly1305AeadCore.OpenRfc8439), kernel);
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing draws the keystream as it comes, planned for each kernel,
    /// except where its draws are estimated to cost less: so on the block function it always draws as it comes.
    /// </summary>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void OpenRfc8439_WhenPlannedForEachKernel_ShouldCostNoMoreThanDrawingAsItComes(string kernel)
    {
        AssertPlannedCostsNoMoreThanAsItComes(nameof(Poly1305AeadCore.OpenRfc8439), kernel);
    }

    /// <summary>
    /// Verifies that opening under the RFC 8439 framing, planned for each kernel, makes draws whose estimated cost is
    /// the one the plan chose them by.
    /// </summary>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void OpenRfc8439_WhenPlannedForEachKernel_ShouldCostWhatThePlanEstimates(string kernel)
    {
        AssertPlannedCostsWhatThePlanEstimates(nameof(Poly1305AeadCore.OpenRfc8439), kernel);
    }
}
