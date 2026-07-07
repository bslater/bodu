// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.Biham.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    // ── Biham Tiger / Tiger2 reference known-answer tests ─────────────────────────────────────
    //
    // Loaded dynamically from the embedded labelled m:/h: reference file, which carries Ross Anderson
    // & Eli Biham's additional Tiger vectors (cs.technion.ac.il/~biham/Reports/Tiger) and the NESSIE
    // Tiger / Tiger2 Set-1 vectors. Tiger uses 0x01 padding; Tiger2 uses 0x80 padding — the reader
    // splits the two variants via the file's `option: variant = tiger2` switch.

    /// <summary>Resource name of the embedded Biham Tiger / Tiger2 reference file.</summary>
    private const string BihamResourceName = "Bodu.Security.Cryptography.Tiger.biham-tiger2-vectors.txt";

    /// <summary>
    /// Loads the standard Tiger (0x01-padding) reference vectors from the embedded file.
    /// </summary>
    /// <returns>One row per Tiger vector; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    private static IEnumerable<object[]> TigerBihamVectors() => LoadBiham(tiger2: false, "Biham Tiger");

    /// <summary>
    /// Loads the Tiger2 (0x80-padding) reference vectors from the embedded file.
    /// </summary>
    /// <returns>One row per Tiger2 vector; each row contains a single <see cref="MessageDigestKnownAnswer" />.</returns>
    private static IEnumerable<object[]> Tiger2BihamVectors() => LoadBiham(tiger2: true, "Biham Tiger2");

    /// <summary>Reads the embedded Biham reference file for the requested padding variant.</summary>
    /// <param name="tiger2"><see langword="true" /> for the Tiger2 rows; otherwise the Tiger rows.</param>
    /// <param name="source">The citation propagated into each emitted vector's name.</param>
    /// <returns>One <see cref="DynamicDataAttribute" /> row per matching vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> LoadBiham(bool tiger2, string source)
    {
        using Stream stream = typeof(TigerTests).Assembly.GetManifestResourceStream(BihamResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{BihamResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (MessageDigestKnownAnswer vector in TigerReferenceKatReader.Read(stream, tiger2, source))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the reference record.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="MessageDigestKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetTigerBihamVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="Tiger" /> in the standard (0x01-padding) 192-bit configuration reproduces the exact
    /// Biham / NESSIE Tiger reference digest for every vector in the embedded reference file.
    /// </summary>
    /// <param name="vector">The Tiger-192 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(TigerBihamVectors), DynamicDataDisplayName = nameof(GetTigerBihamVectorDisplayName))]
    public void ComputeHash_WithBihamTigerVector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var tiger = new Tiger(192) { Variant = TigerHashingVariant.Tiger };
        byte[] actual = tiger.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: Tiger-192 must match the reference digest for a {vector.Message.Length}-byte message.");
    }

    /// <summary>
    /// Verifies that <see cref="Tiger" /> in the Tiger2 (0x80-padding) 192-bit configuration reproduces the exact
    /// NESSIE Tiger2 reference digest for every vector in the embedded reference file.
    /// </summary>
    /// <param name="vector">The Tiger2-192 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Tiger2BihamVectors), DynamicDataDisplayName = nameof(GetTigerBihamVectorDisplayName))]
    public void ComputeHash_WithBihamTiger2Vector_ShouldMatchReferenceDigest(MessageDigestKnownAnswer vector)
    {
        using var tiger = new Tiger(192) { Variant = TigerHashingVariant.Tiger2 };
        byte[] actual = tiger.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: Tiger2-192 must match the reference digest for a {vector.Message.Length}-byte message.");
    }
}
