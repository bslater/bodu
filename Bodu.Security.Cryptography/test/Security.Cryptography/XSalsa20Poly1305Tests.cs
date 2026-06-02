// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="XSalsa20Poly1305" /> (NaCl secretbox) construction. Tests are partitioned
/// across the following partial files:
/// <list type="bullet">
/// <item><description><c>XSalsa20Poly1305Tests.Ctors.cs</c> — constructor null and size validation.</description></item>
/// <item><description><c>XSalsa20Poly1305Tests.EncryptDecrypt.cs</c> — round-trip, sizing, lifecycle, AAD rejection.</description></item>
/// <item><description><c>XSalsa20Poly1305Tests.Tamper.cs</c> — authentication-failure behaviour.</description></item>
/// </list>
/// The libsodium reference known-answer vector is exercised in <see cref="StreamAeadKnownAnswerTests" />.
/// </summary>
[TestClass]
public partial class XSalsa20Poly1305Tests
{
    private static readonly byte[] s_validKey = new byte[XSalsa20Poly1305.KeySize / 8];
    private static readonly byte[] s_validNonce = new byte[XSalsa20Poly1305.NonceSize / 8];

    static XSalsa20Poly1305Tests()
    {
        for (var i = 0; i < s_validKey.Length; i++) s_validKey[i] = (byte)i;
        for (var i = 0; i < s_validNonce.Length; i++) s_validNonce[i] = (byte)(i + 0x40);
    }

    /// <summary>
    /// Verifies that a message encrypted with <see cref="XSalsa20Poly1305" /> round-trips back to the original
    /// plaintext through a freshly constructed decryptor.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Encrypt_WhenRoundTripped_ShouldRecoverPlaintext()
    {
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        byte[] sealed_;
        using (var enc = new XSalsa20Poly1305(s_validKey, s_validNonce))
            sealed_ = enc.Encrypt(plaintext);

        byte[] recovered;
        using (var dec = new XSalsa20Poly1305(s_validKey, s_validNonce))
            recovered = dec.Decrypt(sealed_);

        CollectionAssert.AreEqual(plaintext, recovered);
    }
}
