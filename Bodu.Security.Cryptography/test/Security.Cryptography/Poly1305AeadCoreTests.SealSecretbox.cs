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

        foreach (int length in Enumerable.Range(0, 300).Concat([1024, 1029, 4099]))
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
}
