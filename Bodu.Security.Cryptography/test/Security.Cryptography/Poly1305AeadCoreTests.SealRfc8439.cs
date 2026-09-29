// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.SealRfc8439.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305AeadCore.SealRfc8439" /> driven by a raw ChaCha20 engine reproduces the
    /// ciphertext and tag mandated by RFC 8439 Section 2.8.2.
    /// </summary>
    [TestMethod]
    public void SealRfc8439_WhenGivenRfc8439Vector_ShouldProduceExpectedCiphertextAndTag()
    {
        var engine = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);
        byte[] output = new byte[s_plaintext.Length + Poly1305AeadCore.TagBytes];

        int written = Poly1305AeadCore.SealRfc8439(engine, s_associatedData, s_plaintext, output);

        Assert.AreEqual(output.Length, written);
        CollectionAssert.AreEqual(s_ciphertext, output.AsSpan(0, s_plaintext.Length).ToArray());
        CollectionAssert.AreEqual(s_tag, output.AsSpan(s_plaintext.Length).ToArray());
    }

    /// <summary>
    /// Verifies that sealing under the RFC 8439 framing with an engine that produces keystream in bulk matches sealing
    /// with the same engine one block at a time, for every message length from empty to past the widest kernel's run.
    /// </summary>
    [TestMethod]
    public void SealRfc8439_WhenEngineHasNoBulkPath_ShouldMatchTheBulkEngine()
    {
        var random = new Random(0x5EA1_0001);
        byte[] associatedData = new byte[13];
        random.NextBytes(associatedData);

        foreach (int length in MessageLengths)
        {
            byte[] plaintext = new byte[length];
            random.NextBytes(plaintext);
            byte[] expected = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] actual = new byte[length + Poly1305AeadCore.TagBytes];
            using var single = new SingleBlockStreamCipher(new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0));
            using var bulk = new ChaCha20StreamCipher(s_key, s_nonce, initialCounter: 0);

            _ = Poly1305AeadCore.SealRfc8439(single, associatedData, plaintext, expected);
            _ = Poly1305AeadCore.SealRfc8439(bulk, associatedData, plaintext, actual);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that sealing under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value reproduces the ciphertext and tag mandated by RFC 8439 Section 2.8.2.
    /// </summary>
    [TestMethod]
    public void SealRfc8439_WhenKeystreamIsAValue_ShouldProduceExpectedCiphertextAndTag()
    {
        ChaCha20Core.Keystream keystream = default;
        keystream.Initialize(s_key, s_nonce, counter: 0);
        byte[] output = new byte[s_plaintext.Length + Poly1305AeadCore.TagBytes];

        int written = Poly1305AeadCore.SealRfc8439(ref keystream, s_associatedData, s_plaintext, output);

        Assert.AreEqual(output.Length, written);
        CollectionAssert.AreEqual(Rfc8439CiphertextWithTag(), output);
    }

    /// <summary>
    /// Verifies that sealing under the RFC 8439 framing with the keystream drawn from a
    /// <see cref="ChaCha20Core.Keystream" /> value matches sealing with a ChaCha20 engine, for every message length from
    /// empty to past the widest kernel's run and associated data of every length up to three blocks.
    /// </summary>
    [TestMethod]
    public void SealRfc8439_WhenKeystreamIsAValue_ShouldMatchTheEngine()
    {
        var random = new Random(0x5EA1_0004);

        foreach (int length in MessageLengths)
        {
            byte[] key = NextBytes(random, ChaCha20Core.KeyBytes);
            byte[] nonce = NextBytes(random, ChaCha20Core.NonceBytes);
            byte[] associatedData = NextBytes(random, length % 49);
            byte[] plaintext = NextBytes(random, length);
            byte[] expected = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] actual = new byte[length + Poly1305AeadCore.TagBytes];
            using var engine = new ChaCha20StreamCipher(key, nonce, initialCounter: 0);
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(key, nonce, counter: 0);

            _ = Poly1305AeadCore.SealRfc8439(engine, associatedData, plaintext, expected);
            _ = Poly1305AeadCore.SealRfc8439(ref keystream, associatedData, plaintext, actual);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that sealing under the RFC 8439 framing with the keystream's draws planned for each kernel matches
    /// sealing with a ChaCha20 engine, for every message length from empty to past the longest drawn in one pass and a
    /// longer one.
    /// </summary>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void SealRfc8439_WhenPlannedForEachKernel_ShouldMatchTheEngine(string kernel)
    {
        AssertPlannedMatchesTheEngine(nameof(Poly1305AeadCore.SealRfc8439), kernel);
    }

    /// <summary>
    /// Verifies that sealing under the RFC 8439 framing draws the keystream as it comes, planned for each kernel,
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
    public void SealRfc8439_WhenPlannedForEachKernel_ShouldCostNoMoreThanDrawingAsItComes(string kernel)
    {
        AssertPlannedCostsNoMoreThanAsItComes(nameof(Poly1305AeadCore.SealRfc8439), kernel);
    }

    /// <summary>
    /// Verifies that sealing under the RFC 8439 framing, planned for each kernel, makes draws whose estimated cost is
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
    public void SealRfc8439_WhenPlannedForEachKernel_ShouldCostWhatThePlanEstimates(string kernel)
    {
        AssertPlannedCostsWhatThePlanEstimates(nameof(Poly1305AeadCore.SealRfc8439), kernel);
    }
}
