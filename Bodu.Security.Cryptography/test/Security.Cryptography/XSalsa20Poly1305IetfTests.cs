// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305IetfTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="XSalsa20Poly1305Ietf" /> construction. Tests are partitioned across the
/// following partial files:
/// <list type="bullet">
/// <item><description><c>XSalsa20Poly1305IetfTests.Ctors.cs</c> — constructor null and size validation.</description></item>
/// <item><description><c>XSalsa20Poly1305IetfTests.EncryptDecrypt.cs</c> — round-trip, sizing, lifecycle.</description></item>
/// <item><description><c>XSalsa20Poly1305IetfTests.Tamper.cs</c> — authentication-failure behaviour.</description></item>
/// </list>
/// The derived-oracle correctness check lives in <see cref="StreamAeadKnownAnswerTests" />.
/// </summary>
[TestClass]
public partial class XSalsa20Poly1305IetfTests
{
    private static readonly byte[] s_validKey = new byte[XSalsa20Poly1305Ietf.KeySize / 8];
    private static readonly byte[] s_validNonce = new byte[XSalsa20Poly1305Ietf.NonceSize / 8];

    static XSalsa20Poly1305IetfTests()
    {
        for (var i = 0; i < s_validKey.Length; i++) s_validKey[i] = (byte)i;
        for (var i = 0; i < s_validNonce.Length; i++) s_validNonce[i] = (byte)(i + 0x40);
    }

    /// <summary>
    /// Verifies that a message encrypted with <see cref="XSalsa20Poly1305Ietf" /> round-trips back to the original
    /// plaintext through a freshly constructed decryptor.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Encrypt_WhenRoundTripped_ShouldRecoverPlaintext()
    {
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var aad = new byte[] { 0xaa, 0xbb };

        byte[] sealed_;
        using (var enc = new XSalsa20Poly1305Ietf(s_validKey, s_validNonce))
            sealed_ = enc.Encrypt(plaintext, (ReadOnlySpan<byte>)aad);

        byte[] recovered;
        using (var dec = new XSalsa20Poly1305Ietf(s_validKey, s_validNonce))
            recovered = dec.Decrypt(sealed_, (ReadOnlySpan<byte>)aad);

        CollectionAssert.AreEqual(plaintext, recovered);
    }
}
