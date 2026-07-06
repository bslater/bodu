// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.Nessie.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    // ── Project NESSIE Tiger-192 known-answer tests ───────────────────────────────────────────
    //
    // Loaded dynamically from the embedded Project NESSIE Tiger test-vector file (Tiger, 0x01 padding,
    // 192-bit). The NessieHashKatReader decodes the byte-message rows across all four sets — Set 1
    // canonical strings, Set 2 byte-aligned zero-bit strings, Set 3 patterned 512-bit strings — and
    // skips the bit-unaligned and Set-4 iterated rows that are not a plain byte message.

    /// <summary>Resource name of the embedded NESSIE Tiger reference file.</summary>
    private const string NessieResourceName = "Bodu.Security.Cryptography.Tiger.test-vectors-nessie-format.dat";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string NessieSource = "NESSIE Tiger";

    /// <summary>
    /// Loads every decodable Tiger-192 vector from the embedded NESSIE file and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per decodable NESSIE record; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded NESSIE resource cannot be located.</exception>
    private static IEnumerable<object[]> TigerNessieVectors()
    {
        using Stream stream = typeof(TigerTests).Assembly.GetManifestResourceStream(NessieResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{NessieResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (MessageDigestKnownAnswer vector in NessieHashKatReader.Read(stream, NessieSource))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the NESSIE set and index.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetTigerNessieVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Tiger" /> in the standard (0x01-padding) 192-bit configuration reproduces the exact
    /// NESSIE Tiger reference digest for every decodable vector in the embedded reference file.
    /// </summary>
    /// <param name="vector">The NESSIE Tiger-192 vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(TigerNessieVectors), DynamicDataDisplayName = nameof(GetTigerNessieVectorDisplayName))]
    public void ComputeHash_WithNessieTigerVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var tiger = new Tiger(192) { Variant = TigerHashingVariant.Tiger };
        byte[] actual = tiger.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: Tiger-192 must match the NESSIE reference digest for a {vector.Message.Length}-byte message.");
    }
}
