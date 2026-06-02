// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305AeadTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the <see cref="IStreamAeadTransform" /> contract for the Bodu-defined <see cref="XSalsa20Poly1305Aead" />
/// construction. The derived-oracle and frozen-vector checks live in <see cref="StreamAeadKnownAnswerTests" />.
/// </summary>
[TestClass]
public sealed class XSalsa20Poly1305AeadTests
    : StreamAeadTransformContractTests<XSalsa20Poly1305Aead>
{
    /// <inheritdoc />
    protected override XSalsa20Poly1305Aead Create(byte[] key, byte[] nonce) => new(key, nonce);

    /// <summary>
    /// Verifies that a message round-trips through a freshly constructed decryptor.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Encrypt_WhenRoundTripped_ShouldRecoverPlaintext()
    {
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var aad = new byte[] { 0xaa, 0xbb };

        byte[] sealed_;
        using (var enc = new XSalsa20Poly1305Aead(Key(), Nonce()))
            sealed_ = enc.Encrypt(plaintext, (ReadOnlySpan<byte>)aad);

        byte[] recovered;
        using (var dec = new XSalsa20Poly1305Aead(Key(), Nonce()))
            recovered = dec.Decrypt(sealed_, (ReadOnlySpan<byte>)aad);

        CollectionAssert.AreEqual(plaintext, recovered);
    }
}
