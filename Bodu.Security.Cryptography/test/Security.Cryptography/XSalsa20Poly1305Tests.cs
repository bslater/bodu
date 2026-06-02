// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the <see cref="IStreamAeadTransform" /> contract for the <see cref="XSalsa20Poly1305" /> (NaCl secretbox)
/// construction. The libsodium reference vector and the libsodium-layout interop check live in
/// <see cref="StreamAeadKnownAnswerTests" />; layout-conversion round-trips are in
/// <c>XSalsa20Poly1305Tests.Secretbox.cs</c>.
/// </summary>
[TestClass]
public sealed partial class XSalsa20Poly1305Tests
    : StreamAeadTransformContractTests<XSalsa20Poly1305>
{
    /// <inheritdoc />
    protected override bool SupportsAssociatedData => false;

    /// <inheritdoc />
    protected override XSalsa20Poly1305 Create(byte[] key, byte[] nonce) => new(key, nonce);

    /// <summary>
    /// Verifies that a message round-trips through a freshly constructed decryptor.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Encrypt_WhenRoundTripped_ShouldRecoverPlaintext()
    {
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        byte[] sealed_;
        using (var enc = new XSalsa20Poly1305(Key(), Nonce()))
            sealed_ = enc.Encrypt(plaintext);

        byte[] recovered;
        using (var dec = new XSalsa20Poly1305(Key(), Nonce()))
            recovered = dec.Decrypt(sealed_);

        CollectionAssert.AreEqual(plaintext, recovered);
    }
}
