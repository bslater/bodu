// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.ReferenceKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class WhirlpoolTests
{
    // ── Official OpenSSL / ISO-IEC 10118-3 Whirlpool reference tests ───────────────────────────
    //
    // Loaded dynamically from the embedded OpenSSL evptests reference file (test/recipes/
    // 30-test_evp_data/evpmd_whirlpool.txt). These pin the standardized ISO/IEC 10118-3 revision —
    // the default Whirlpool (WhirlpoolInfo3) — against the published ISO test-message set, including
    // the one-million-'a' stress vector.

    /// <summary>Resource name of the embedded OpenSSL Whirlpool reference vector file.</summary>
    private const string WhirlpoolOpenSslResourceName = "Bodu.Security.Cryptography.Whirlpool.openssl-evptests.txt";

    /// <summary>
    /// Loads every Whirlpool vector from the embedded OpenSSL evptests file and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per vector; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> WhirlpoolOpenSslReferenceVectors()
    {
        using Stream stream = typeof(WhirlpoolTests).Assembly.GetManifestResourceStream(WhirlpoolOpenSslResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{WhirlpoolOpenSslResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in OpenSslDigestKatReader.Read(stream, "whirlpool", "Whirlpool ISO KAT"))
            yield return new object[] { vector };
    }

    /// <summary>Produces a human-readable display name for a KAT row.</summary>
    /// <param name="methodInfo">The test method's reflection info.</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetWhirlpoolOpenSslVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that the default <see cref="Whirlpool" /> (ISO/IEC 10118-3 revision) reproduces the exact digest from
    /// every entry of the official OpenSSL Whirlpool reference vectors, including the one-million-'a' input.
    /// </summary>
    /// <param name="vector">The Whirlpool reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(WhirlpoolOpenSslReferenceVectors), DynamicDataDisplayName = nameof(GetWhirlpoolOpenSslVectorDisplayName))]
    public void ComputeHash_WithOpenSslWhirlpoolKatVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var whirlpool = new Whirlpool();

        byte[] actual = whirlpool.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: Whirlpool must match the OpenSSL/ISO reference digest.");
    }
}
