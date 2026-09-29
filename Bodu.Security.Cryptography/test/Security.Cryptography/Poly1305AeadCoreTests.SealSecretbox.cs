// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.SealSecretbox.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that sealing under the secretbox framing with an engine that produces keystream in bulk matches sealing
    /// with the same engine one block at a time, for every message length from empty to past the widest kernel's run,
    /// including those that end within the counter-0 block's trailing 32 bytes.
    /// </summary>
    [TestMethod]
    public void SealSecretbox_WhenEngineHasNoBulkPath_ShouldMatchTheBulkEngine()
    {
        var random = new Random(0x5EA1_0003);
        byte[] key = new byte[32];
        byte[] nonce = new byte[8];
        random.NextBytes(key);
        random.NextBytes(nonce);

        foreach (int length in MessageLengths)
        {
            byte[] plaintext = new byte[length];
            random.NextBytes(plaintext);
            byte[] expected = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] actual = new byte[length + Poly1305AeadCore.TagBytes];
            using var single = new SingleBlockStreamCipher(new Salsa20StreamCipher(key, nonce, initialCounter: 0));
            using var bulk = new Salsa20StreamCipher(key, nonce, initialCounter: 0);

            _ = Poly1305AeadCore.SealSecretbox(single, plaintext, expected);
            _ = Poly1305AeadCore.SealSecretbox(bulk, plaintext, actual);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that sealing under the secretbox framing with the keystream drawn from a
    /// <see cref="Salsa20Core.Keystream" /> value matches sealing with a Salsa20 engine, for every message length from
    /// empty to past the widest kernel's run, including those that end within the counter-0 block's trailing 32 bytes.
    /// </summary>
    [TestMethod]
    public void SealSecretbox_WhenKeystreamIsAValue_ShouldMatchTheEngine()
    {
        var random = new Random(0x5EA1_0005);

        foreach (int length in MessageLengths)
        {
            byte[] key = NextBytes(random, Salsa20Core.Key256Bytes);
            byte[] nonce = NextBytes(random, Salsa20Core.NonceBytes);
            byte[] plaintext = NextBytes(random, length);
            byte[] expected = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] actual = new byte[length + Poly1305AeadCore.TagBytes];
            using var engine = new Salsa20StreamCipher(key, nonce, initialCounter: 0);
            Salsa20Core.Keystream keystream = default;
            keystream.Initialize(key, nonce, counter: 0);

            _ = Poly1305AeadCore.SealSecretbox(engine, plaintext, expected);
            _ = Poly1305AeadCore.SealSecretbox(ref keystream, plaintext, actual);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that sealing under the secretbox framing with the keystream's draws planned for each kernel matches
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
    public void SealSecretbox_WhenPlannedForEachKernel_ShouldMatchTheEngine(string kernel)
    {
        AssertPlannedMatchesTheEngine(nameof(Poly1305AeadCore.SealSecretbox), kernel);
    }

    /// <summary>
    /// Verifies that sealing under the secretbox framing draws the keystream as it comes, planned for each kernel,
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
    public void SealSecretbox_WhenPlannedForEachKernel_ShouldCostNoMoreThanDrawingAsItComes(string kernel)
    {
        AssertPlannedCostsNoMoreThanAsItComes(nameof(Poly1305AeadCore.SealSecretbox), kernel);
    }

    /// <summary>
    /// Verifies that sealing under the secretbox framing, planned for each kernel, makes draws whose estimated cost is
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
    public void SealSecretbox_WhenPlannedForEachKernel_ShouldCostWhatThePlanEstimates(string kernel)
    {
        AssertPlannedCostsWhatThePlanEstimates(nameof(Poly1305AeadCore.SealSecretbox), kernel);
    }
}
