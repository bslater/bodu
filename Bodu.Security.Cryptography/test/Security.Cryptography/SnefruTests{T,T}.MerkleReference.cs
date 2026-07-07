// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SnefruTests{T,T}.MerkleReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class Snefru128Tests
{
    // ── Merkle Snefru 2.5a reference known-answer tests (8-pass) ───────────────────────────────
    //
    // Loaded dynamically from the embedded Snefru 2.5a reference files (Ralph Merkle's hash2.5a
    // distribution): the testSnefru shell script supplies each newline-terminated input via its
    // here-documents, and correctSnefruOutput supplies the matching 128-bit digests. The reference is
    // built at the 8-pass security level (SECURITY_LEVEL=8), which is the level Bodu implements. Note
    // the trailing-newline convention: the input for digit string "N" is "N" followed by 0x0A, and the
    // empty case is a lone 0x0A — reconstructed faithfully from the here-document bodies.

    /// <summary>Resource name of the embedded Snefru 2.5a input script (128-bit set).</summary>
    private const string InputResourceName = "Bodu.Security.Cryptography.Snefru.testSnefru.txt";

    /// <summary>Resource name of the embedded Snefru 2.5a digest listing (128-bit set).</summary>
    private const string DigestResourceName = "Bodu.Security.Cryptography.Snefru.correctSnefruOutput.txt";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string KatSource = "Merkle Snefru 2.5a (8-pass) 128-bit";

    /// <summary>
    /// Loads every Snefru-128 reference vector by pairing the embedded input script with the embedded digest listing.
    /// </summary>
    /// <returns>One row per paired record; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">An embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Snefru128ReferenceVectors()
    {
        using Stream inputStream = typeof(Snefru128Tests).Assembly.GetManifestResourceStream(InputResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{InputResourceName}' is missing.");
        using Stream digestStream = typeof(Snefru128Tests).Assembly.GetManifestResourceStream(DigestResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{DigestResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in MerkleSnefruKatReader.Read(inputStream, digestStream, digestWords: 4, source: KatSource))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the reference record.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetSnefru128VectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Snefru128" /> (128-bit, 8-pass) reproduces the exact digest from Merkle's Snefru 2.5a
    /// reference output for every newline-terminated input in the embedded reference files.
    /// </summary>
    /// <param name="vector">The Snefru-128 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Snefru128ReferenceVectors), DynamicDataDisplayName = nameof(GetSnefru128VectorDisplayName))]
    public void ComputeHash_WithMerkleSnefruReferenceVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var snefru = new Snefru128();
        byte[] actual = snefru.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: Snefru-128 (8-pass) must match Merkle's reference digest.");
    }
}

public partial class Snefru256Tests
{
    // ── Merkle Snefru 2.5a reference known-answer tests (8-pass, OUTPUT_BLOCK_SIZE=8) ──────────
    //
    // Loaded dynamically from the embedded testSnefru256 / correctSnefru256Output reference files
    // (Merkle 2.5a, SECURITY_LEVEL=8, OUTPUT_BLOCK_SIZE=8). Same trailing-newline input convention as
    // the 128-bit set.

    /// <summary>Resource name of the embedded Snefru 2.5a input script (256-bit set).</summary>
    private const string InputResourceName = "Bodu.Security.Cryptography.Snefru.testSnefru256.txt";

    /// <summary>Resource name of the embedded Snefru 2.5a digest listing (256-bit set).</summary>
    private const string DigestResourceName = "Bodu.Security.Cryptography.Snefru.correctSnefru256Output.txt";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string KatSource = "Merkle Snefru 2.5a (8-pass) 256-bit";

    /// <summary>
    /// Loads every Snefru-256 reference vector by pairing the embedded input script with the embedded digest listing.
    /// </summary>
    /// <returns>One row per paired record; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">An embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Snefru256ReferenceVectors()
    {
        using Stream inputStream = typeof(Snefru256Tests).Assembly.GetManifestResourceStream(InputResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{InputResourceName}' is missing.");
        using Stream digestStream = typeof(Snefru256Tests).Assembly.GetManifestResourceStream(DigestResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{DigestResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in MerkleSnefruKatReader.Read(inputStream, digestStream, digestWords: 8, source: KatSource))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the reference record.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetSnefru256VectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Snefru256" /> (256-bit, 8-pass) reproduces the exact digest from Merkle's Snefru 2.5a
    /// reference output for every newline-terminated input in the embedded reference files.
    /// </summary>
    /// <param name="vector">The Snefru-256 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Snefru256ReferenceVectors), DynamicDataDisplayName = nameof(GetSnefru256VectorDisplayName))]
    public void ComputeHash_WithMerkleSnefruReferenceVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var snefru = new Snefru256();
        byte[] actual = snefru.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: Snefru-256 (8-pass) must match Merkle's reference digest.");
    }
}
