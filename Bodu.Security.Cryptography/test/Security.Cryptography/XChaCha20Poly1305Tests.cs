// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20Poly1305Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the <see cref="IStreamAeadTransform" /> contract for <see cref="XChaCha20Poly1305" />. The
/// draft-irtf-cfrg-xchacha reference vector is exercised in <see cref="StreamAeadKnownAnswerTests" />.
/// </summary>
[TestClass]
public sealed class XChaCha20Poly1305Tests
    : StreamAeadTransformContractTests<XChaCha20Poly1305>
{
    /// <inheritdoc />
    protected override XChaCha20Poly1305 Create(byte[] key, byte[] nonce) => new(key, nonce);

    /// <summary>
    /// Verifies that a message round-trips through a freshly constructed decryptor.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Encrypt_WhenRoundTripped_ShouldRecoverPlaintext()
    {
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        byte[] sealed_;
        using (var enc = new XChaCha20Poly1305(Key(), Nonce()))
            sealed_ = enc.Encrypt(plaintext);

        byte[] recovered;
        using (var dec = new XChaCha20Poly1305(Key(), Nonce()))
            recovered = dec.Decrypt(sealed_);

        CollectionAssert.AreEqual(plaintext, recovered);
    }
}
