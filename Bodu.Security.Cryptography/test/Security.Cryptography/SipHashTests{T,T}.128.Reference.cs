// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests{T,T}.128.Reference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class SipHash128Tests
{
    // ── SipHash-2-4-128 reference known-answer tests ──────────────────────────────────────────
    //
    // Loaded dynamically from the embedded SipHash reference implementation source (github.com/veorq/
    // SipHash, vectors.h, table vectors_sip128). The 128-bit variant is not in the 2012 paper (it was
    // added later for application needs), so vectors.h is the authoritative source. Scheme: key = the
    // bytes 0x00..0x0F, and vector i is the tag over the i-byte message 0x00..0x(i-1), i in 0..63.

    /// <summary>Resource name of the embedded veorq/SipHash <c>vectors.h</c> reference source.</summary>
    private const string SipHashVectorsResourceName = "Bodu.Security.Cryptography.SipHash.vectors.h";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string SipHash128Source = "veorq/SipHash vectors.h";

    /// <summary>
    /// Loads every SipHash-2-4-128 reference tag from the embedded <c>vectors.h</c> and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per table entry; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded vectors resource cannot be located.</exception>
    private static IEnumerable<object[]> SipHash128ReferenceVectors()
    {
        using Stream stream = typeof(SipHash128Tests).Assembly.GetManifestResourceStream(SipHashVectorsResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{SipHashVectorsResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (MessageDigestKnownAnswer vector in
            SipHashReferenceVectorsReader.Read(stream, "vectors_sip128", outputBytes: 16, source: SipHash128Source))
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
    public static string GetSipHash128VectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="SipHash128" /> in its default SipHash-2-4 configuration, keyed with the reference key
    /// 0x00..0x0F, reproduces the exact 128-bit tag from every row of the veorq/SipHash <c>vectors_sip128</c> table.
    /// </summary>
    /// <param name="vector">The SipHash-2-4-128 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SipHash128ReferenceVectors), DynamicDataDisplayName = nameof(GetSipHash128VectorDisplayName))]
    public void ComputeHash_WithVeorqSipHash128Vector_ShouldMatchReferenceTag(MessageDigestKnownAnswer vector)
    {
        using var sip = new SipHash128 { Key = vector.Key! };
        byte[] actual = sip.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: SipHash-2-4-128 must match the veorq/SipHash reference tag.");
    }
}
