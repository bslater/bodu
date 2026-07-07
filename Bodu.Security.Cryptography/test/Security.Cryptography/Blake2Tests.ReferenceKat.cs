// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2Tests.ReferenceKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class Blake2bTests
{
    // ── Official BLAKE2 blake2-kat.json reference tests (BLAKE2b) ──────────────────────────────
    //
    // Loaded dynamically from the embedded official BLAKE2 test-vector file (github.com/BLAKE2/
    // BLAKE2, testvectors/blake2-kat.json). Each entry is a 512-bit BLAKE2b digest over an
    // incrementing message, in both the unkeyed and full-length-keyed (MAC) modes.

    /// <summary>Resource name of the embedded BLAKE2 reference vector file.</summary>
    private const string Blake2KatResourceName = "Bodu.Security.Cryptography.Blake2.blake2-kat.json";

    /// <summary>
    /// Loads every BLAKE2b vector from the embedded blake2-kat.json and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per vector; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Blake2bReferenceVectors()
    {
        using Stream stream = typeof(Blake2bTests).Assembly.GetManifestResourceStream(Blake2KatResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Blake2KatResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in Blake2KatReader.Read(stream, "blake2b", "BLAKE2 KAT"))
            yield return new object[] { vector };
    }

    /// <summary>Produces a human-readable display name for a KAT row.</summary>
    /// <param name="methodInfo">The test method's reflection info.</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetBlake2bVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Blake2b" /> at 512-bit output reproduces the exact digest from every BLAKE2b entry of
    /// the official blake2-kat.json, in both unkeyed and keyed (MAC) modes.
    /// </summary>
    /// <param name="vector">The BLAKE2b reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Blake2bReferenceVectors), DynamicDataDisplayName = nameof(GetBlake2bVectorDisplayName))]
    public void ComputeHash_WithBlake2KatVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var blake = new Blake2b(vector.Digest.Length * 8);
        if (vector.Key is not null) blake.Key = vector.Key;

        byte[] actual = blake.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: BLAKE2b must match the blake2-kat.json reference digest.");
    }
}

public partial class Blake2sTests
{
    // ── Official BLAKE2 blake2-kat.json reference tests (BLAKE2s) ──────────────────────────────

    /// <summary>Resource name of the embedded BLAKE2 reference vector file.</summary>
    private const string Blake2KatResourceName = "Bodu.Security.Cryptography.Blake2.blake2-kat.json";

    /// <summary>
    /// Loads every BLAKE2s vector from the embedded blake2-kat.json and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per vector; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Blake2sReferenceVectors()
    {
        using Stream stream = typeof(Blake2sTests).Assembly.GetManifestResourceStream(Blake2KatResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Blake2KatResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in Blake2KatReader.Read(stream, "blake2s", "BLAKE2 KAT"))
            yield return new object[] { vector };
    }

    /// <summary>Produces a human-readable display name for a KAT row.</summary>
    /// <param name="methodInfo">The test method's reflection info.</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetBlake2sVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Blake2s" /> at 256-bit output reproduces the exact digest from every BLAKE2s entry of
    /// the official blake2-kat.json, in both unkeyed and keyed (MAC) modes.
    /// </summary>
    /// <param name="vector">The BLAKE2s reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Blake2sReferenceVectors), DynamicDataDisplayName = nameof(GetBlake2sVectorDisplayName))]
    public void ComputeHash_WithBlake2KatVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var blake = new Blake2s(vector.Digest.Length * 8);
        if (vector.Key is not null) blake.Key = vector.Key;

        byte[] actual = blake.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: BLAKE2s must match the blake2-kat.json reference digest.");
    }
}
