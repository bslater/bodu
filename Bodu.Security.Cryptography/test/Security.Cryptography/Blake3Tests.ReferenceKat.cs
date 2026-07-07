// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.ReferenceKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class Blake3Tests
{
    // ── Official BLAKE3 test_vectors.json reference tests ──────────────────────────────────────
    //
    // Loaded dynamically from the embedded official BLAKE3 test-vector file (github.com/BLAKE3-
    // team/BLAKE3, test_vectors/test_vectors.json). Each case fixes an input length; the reference
    // input is the repeating 251-byte ramp input[i] = i mod 251. Bodu's Blake3 supports the standard
    // unkeyed hash mode at the fixed 256-bit output only, so we pin the first 32 bytes of the
    // extended `hash` output; the keyed_hash and derive_key modes are not exercised.

    /// <summary>Resource name of the embedded BLAKE3 reference vector file.</summary>
    private const string Blake3KatResourceName = "Bodu.Security.Cryptography.Blake3.test_vectors.json";

    /// <summary>
    /// Loads every BLAKE3 vector from the embedded test_vectors.json and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows, each digest truncated to 256 bits.
    /// </summary>
    /// <returns>One row per vector; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Blake3ReferenceVectors()
    {
        using Stream stream = typeof(Blake3Tests).Assembly.GetManifestResourceStream(Blake3KatResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Blake3KatResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in Blake3KatReader.Read(stream, 32, "BLAKE3 KAT"))
            yield return new object[] { vector };
    }

    /// <summary>Produces a human-readable display name for a KAT row.</summary>
    /// <param name="methodInfo">The test method's reflection info.</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetBlake3VectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Blake3" /> reproduces the exact 256-bit digest from every entry of the official BLAKE3
    /// test_vectors.json for the standard unkeyed hash mode.
    /// </summary>
    /// <param name="vector">The BLAKE3 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Blake3ReferenceVectors), DynamicDataDisplayName = nameof(GetBlake3VectorDisplayName))]
    public void ComputeHash_WithBlake3KatVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var blake = new Blake3();

        byte[] actual = blake.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: BLAKE3 must match the test_vectors.json reference digest.");
    }
}
