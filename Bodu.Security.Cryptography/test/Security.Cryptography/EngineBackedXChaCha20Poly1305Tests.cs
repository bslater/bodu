// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EngineBackedXChaCha20Poly1305Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the <see cref="IStreamAeadTransform" /> contract through the engine path of
/// <see cref="Poly1305AeadTransform" />, which a type derived outside the library takes, and holds that path to the
/// allocation-free one <see cref="XChaCha20Poly1305" /> takes.
/// </summary>
[TestClass]
public sealed class EngineBackedXChaCha20Poly1305Tests
    : StreamAeadTransformContractTests<EngineBackedXChaCha20Poly1305>
{
    /// <inheritdoc />
    /// <value><see langword="false" />: every message creates its engine.</value>
    protected override bool AllocatesNothingPerMessage => false;

    /// <summary>
    /// Verifies that sealing through the engine path produces, for every message length to past the widest kernel's
    /// run, the output of <see cref="XChaCha20Poly1305" /> under the same key, nonce and associated data.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenGivenTheSameKeyAndNonce_ShouldMatchXChaCha20Poly1305()
    {
        var random = new Random(0x0EAD_0001);
        byte[] key = Key();
        byte[] nonce = Nonce();

        foreach (int length in Enumerable.Range(0, 200).Concat([1024, 1029, 4099]))
        {
            byte[] plaintext = new byte[length];
            byte[] associatedData = new byte[length % 37];
            random.NextBytes(plaintext);
            random.NextBytes(associatedData);
            byte[] expected = new byte[length + 16];
            byte[] actual = new byte[length + 16];

            using (var sealedType = new XChaCha20Poly1305(key, nonce))
                _ = sealedType.Encrypt(plaintext, expected, associatedData);

            using (EngineBackedXChaCha20Poly1305 engineBacked = Create(key, nonce))
                _ = engineBacked.Encrypt(plaintext, actual, associatedData);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that opening through the engine path recovers a message <see cref="XChaCha20Poly1305" /> sealed.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenSealedByXChaCha20Poly1305_ShouldRecoverPlaintext()
    {
        var random = new Random(0x0EAD_0002);
        byte[] plaintext = new byte[1029];
        byte[] associatedData = new byte[19];
        random.NextBytes(plaintext);
        random.NextBytes(associatedData);
        byte[] sealedMessage = new byte[plaintext.Length + 16];
        byte[] recovered = new byte[plaintext.Length];

        using (var sealedType = new XChaCha20Poly1305(Key(), Nonce()))
            _ = sealedType.Encrypt(plaintext, sealedMessage, associatedData);

        using (EngineBackedXChaCha20Poly1305 engineBacked = Create(Key(), Nonce()))
            _ = engineBacked.Decrypt(sealedMessage, recovered, associatedData);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <inheritdoc />
    protected override EngineBackedXChaCha20Poly1305 Create(byte[] key, byte[] nonce) => new(key, nonce);
}
