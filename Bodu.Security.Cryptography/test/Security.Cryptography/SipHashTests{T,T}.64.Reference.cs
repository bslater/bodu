// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests{T,T}.64.Reference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class SipHash64Tests
{
    // ── SipHash-2-4 (64-bit) reference known-answer tests ─────────────────────────────────────
    //
    // Loaded dynamically from the embedded SipHash reference implementation source (github.com/veorq/
    // SipHash, vectors.h, table vectors_sip64) — the vectors from the 2012 Aumasson-Bernstein paper's
    // reference code. Scheme: key = the bytes 0x00..0x0F, and vector i is the 64-bit tag over the
    // i-byte message 0x00..0x(i-1), i in 0..63.

    /// <summary>Resource name of the embedded veorq/SipHash <c>vectors.h</c> reference source.</summary>
    private const string SipHashVectorsResourceName = "Bodu.Security.Cryptography.SipHash.vectors.h";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string SipHash64Source = "veorq/SipHash vectors.h";

    /// <summary>
    /// Loads every SipHash-2-4 (64-bit) reference tag from the embedded <c>vectors.h</c> and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per table entry; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded vectors resource cannot be located.</exception>
    private static IEnumerable<object[]> SipHash64ReferenceVectors()
    {
        using Stream stream = typeof(SipHash64Tests).Assembly.GetManifestResourceStream(SipHashVectorsResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{SipHashVectorsResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (MessageDigestKnownAnswer vector in
            SipHashReferenceVectorsReader.Read(stream, "vectors_sip64", outputBytes: 8, source: SipHash64Source))
        {
            yield return new object[] { vector };
        }
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the table index.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetSipHash64VectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="SipHash64" /> in its default SipHash-2-4 configuration, keyed with the reference key
    /// 0x00..0x0F, reproduces the exact 64-bit tag from every row of the veorq/SipHash <c>vectors_sip64</c> table.
    /// </summary>
    /// <param name="vector">The SipHash-2-4 (64-bit) reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SipHash64ReferenceVectors), DynamicDataDisplayName = nameof(GetSipHash64VectorDisplayName))]
    public void ComputeHash_WithVeorqSipHash64Vector_ShouldMatchReferenceTag(MessageDigestKnownAnswer vector)
    {
        using var sip = new SipHash64 { Key = vector.Key! };
        byte[] actual = sip.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: SipHash-2-4 (64-bit) must match the veorq/SipHash reference tag.");
    }
}
