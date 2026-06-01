// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks the <see cref="AsconAead128" /> implementation against the full NIST SP 800-232 / ASCON team reference
/// known-answer test corpus for ASCON-AEAD128.
/// </summary>
public partial class AsconAead128Tests
{
    /// <summary>
    /// Resource name of the embedded NIST LWC AEAD KAT file shipped with this test assembly.
    /// </summary>
    private const string KatResourceName = "Bodu.Security.Cryptography.AsconAead128.LWC_AEAD_KAT_128_128.txt";

    /// <summary>
    /// Citation propagated into each emitted vector for diagnostic output on failure.
    /// </summary>
    private const string KatSource = "NIST SP 800-232 / ascon-c LWC_AEAD_KAT_128_128";

    /// <summary>
    /// Loads every AEAD known-answer vector embedded in the test assembly and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per KAT vector; each row contains a single <see cref="AeadKnownAnswerVector" /> object.</returns>
    /// <exception cref="InvalidOperationException">The embedded KAT resource cannot be located.</exception>
    private static IEnumerable<object[]> AsconAead128ReferenceVectors()
    {
        using Stream stream = typeof(AsconAead128Tests).Assembly.GetManifestResourceStream(KatResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{KatResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (AeadKnownAnswerVector vector in NistLwcKatReader.Read(stream, tagLength: 16, source: KatSource))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row, exposing the vector <c>Count</c> in test runners so
    /// failures can be traced back to a specific entry in the embedded KAT file.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="AeadKnownAnswerVector" />).</param>
    /// <returns>A short label that identifies this KAT vector.</returns>
    public static string GetKatVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is AeadKnownAnswerVector v ? v.ToString() : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Encrypt" /> and <see cref="AsconAead128.Decrypt" /> produce the
    /// ciphertext, tag, and recovered plaintext mandated by every vector in the official NIST SP 800-232 / ASCON-team
    /// reference KAT file for ASCON-AEAD128.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(AsconAead128ReferenceVectors), DynamicDataDisplayName = nameof(GetKatVectorDisplayName))]
    [TestCategory("Regression")]
    public void EncryptAndDecrypt_WhenGivenNistKatVector_ShouldMatchExpectedCiphertextTagAndPlaintext(AeadKnownAnswerVector vector)
    {
        // Encrypt
        var ciphertextWithTag = new byte[vector.Plaintext.Length + AsconAead128.TagBytes];
        using (var enc = new AsconAead128(vector.Key, vector.Nonce))
        {
            enc.ProcessAssociatedData(vector.AssociatedData);
            var written = enc.Encrypt(vector.Plaintext, ciphertextWithTag);
            Assert.AreEqual(ciphertextWithTag.Length, written,
                $"{vector}: Encrypt should write plaintext.Length + 16 bytes.");
        }

        var actualCiphertext = ciphertextWithTag.AsSpan(0, vector.Plaintext.Length).ToArray();
        var actualTag = ciphertextWithTag.AsSpan(vector.Plaintext.Length, AsconAead128.TagBytes).ToArray();

        CollectionAssert.AreEqual(vector.Ciphertext, actualCiphertext,
            $"{vector}: ciphertext must match reference.");
        CollectionAssert.AreEqual(vector.Tag, actualTag,
            $"{vector}: authentication tag must match reference.");

        // Decrypt round-trip
        var recovered = new byte[vector.Plaintext.Length];
        using (var dec = new AsconAead128(vector.Key, vector.Nonce))
        {
            dec.ProcessAssociatedData(vector.AssociatedData);
            var written = dec.Decrypt(ciphertextWithTag, recovered);
            Assert.AreEqual(vector.Plaintext.Length, written,
                $"{vector}: Decrypt should return plaintext.Length bytes.");
        }

        CollectionAssert.AreEqual(vector.Plaintext, recovered,
            $"{vector}: Decrypt must recover the reference plaintext.");
    }
}
