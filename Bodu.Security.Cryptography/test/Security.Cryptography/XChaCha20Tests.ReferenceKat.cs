// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20Tests.ReferenceKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class XChaCha20Tests
{
    // ── draft-arciszewski-xchacha-03 reference vectors (embedded file) ────────────────────────
    //
    // Loaded dynamically from the embedded draft text: the Section 2.2.1 HChaCha20 block-function subkey and the
    // Appendix A.3.2 developer-friendly XChaCha20 encryption vector (counter 1).

    /// <summary>Resource name of the embedded draft-arciszewski-xchacha-03 vector file.</summary>
    private const string XChaChaDraftResourceName = "Bodu.Security.Cryptography.XChaCha20.draft-arciszewski-xchacha-03.txt";

    /// <summary>Opens the embedded draft vector file.</summary>
    /// <returns>A readable stream over the draft source.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static Stream OpenXChaChaDraft() =>
        typeof(XChaCha20Tests).Assembly.GetManifestResourceStream(XChaChaDraftResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{XChaChaDraftResourceName}' is missing.");

    /// <summary>
    /// Verifies that the internal HChaCha20 subkey-derivation core reproduces the draft Section 2.2.1 subkey loaded
    /// from the embedded draft file.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void HChaCha20_WhenGivenDraftFileVector_ShouldDeriveExpectedSubkey()
    {
        byte[] key, nonce, expected;
        using (Stream stream = OpenXChaChaDraft())
            (key, nonce, expected) = XChaChaDraftVectorReader.ReadHChaCha20(stream);

        byte[] subkey = new byte[32];
        ChaCha20StreamCipher.HChaCha20(key, nonce, subkey);

        CollectionAssert.AreEqual(expected, subkey, "HChaCha20 subkey mismatch for the draft Section 2.2.1 vector.");
    }

    /// <summary>
    /// Verifies that <see cref="XChaCha20" /> encrypts the draft Appendix A.3.2 plaintext to the published ciphertext,
    /// and that its raw keystream matches the published keystream, both loaded from the embedded draft file.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void CreateEncryptor_WhenGivenDraftAppendixA32Vector_ShouldMatchCiphertextAndKeystream()
    {
        Rfc8439TestVector vector;
        using (Stream stream = OpenXChaChaDraft())
            vector = XChaChaDraftVectorReader.ReadDeveloperFriendly(stream, "A.3.2.  XChaCha20");

        byte[] key = vector.Field("Key");
        byte[] iv = vector.Field("IV");
        byte[] plaintext = vector.Field("Plaintext");
        byte[] expectedKeystream = vector.Field("Keystream");
        byte[] expectedCiphertext = vector.Field("Ciphertext");
        uint counter = vector.InitialBlockCounter ?? 0;

        using (var cipher = new XChaCha20 { InitialCounter = counter })
        using (ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv))
        {
            byte[] actual = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
            CollectionAssert.AreEqual(expectedCiphertext, actual, "XChaCha20 ciphertext mismatch for draft A.3.2.");
        }

        using (var cipher = new XChaCha20 { InitialCounter = counter })
        using (ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv))
        {
            byte[] keystream = encryptor.TransformFinalBlock(new byte[expectedKeystream.Length], 0, expectedKeystream.Length);
            CollectionAssert.AreEqual(expectedKeystream, keystream, "XChaCha20 keystream mismatch for draft A.3.2.");
        }
    }
}
