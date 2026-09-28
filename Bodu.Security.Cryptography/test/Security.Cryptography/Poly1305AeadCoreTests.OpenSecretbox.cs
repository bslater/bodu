// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.OpenSecretbox.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that opening under the secretbox framing with the keystream drawn from a
    /// <see cref="Salsa20Core.Keystream" /> value recovers every plaintext, of every length from empty to past the
    /// widest kernel's run, sealed with a Salsa20 engine one block at a time.
    /// </summary>
    [TestMethod]
    public void OpenSecretbox_WhenKeystreamIsAValue_ShouldRecoverPlaintext()
    {
        var random = new Random(0x5EA1_0006);

        foreach (int length in MessageLengths)
        {
            byte[] key = NextBytes(random, Salsa20Core.Key256Bytes);
            byte[] nonce = NextBytes(random, Salsa20Core.NonceBytes);
            byte[] plaintext = NextBytes(random, length);
            byte[] sealedMessage = new byte[length + Poly1305AeadCore.TagBytes];
            byte[] recovered = new byte[length];
            using var single = new SingleBlockStreamCipher(new Salsa20StreamCipher(key, nonce, initialCounter: 0));
            Salsa20Core.Keystream keystream = default;
            keystream.Initialize(key, nonce, counter: 0);

            _ = Poly1305AeadCore.SealSecretbox(single, plaintext, sealedMessage);
            int written = Poly1305AeadCore.OpenSecretbox(ref keystream, sealedMessage, recovered);

            Assert.AreEqual(length, written, $"length {length}");
            CollectionAssert.AreEqual(plaintext, recovered, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that opening under the secretbox framing throws <see cref="CryptographicException" /> when a ciphertext
    /// byte has been altered, before writing any plaintext.
    /// </summary>
    [TestMethod]
    public void OpenSecretbox_WhenCiphertextIsTampered_ShouldThrowWithoutWritingOutput()
    {
        var random = new Random(0x5EA1_0007);
        byte[] key = NextBytes(random, Salsa20Core.Key256Bytes);
        byte[] nonce = NextBytes(random, Salsa20Core.NonceBytes);
        byte[] sealedMessage = new byte[100 + Poly1305AeadCore.TagBytes];
        using (var engine = new Salsa20StreamCipher(key, nonce, initialCounter: 0))
            _ = Poly1305AeadCore.SealSecretbox(engine, NextBytes(random, 100), sealedMessage);

        sealedMessage[40] ^= 0x80;
        byte[] output = new byte[100];
        Array.Fill(output, (byte)0xCC);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            Salsa20Core.Keystream keystream = default;
            keystream.Initialize(key, nonce, counter: 0);
            _ = Poly1305AeadCore.OpenSecretbox(ref keystream, sealedMessage, output);
        });

        Assert.IsTrue(output.All(value => value == 0xCC), "The output was written before the tag was verified.");
    }
}
