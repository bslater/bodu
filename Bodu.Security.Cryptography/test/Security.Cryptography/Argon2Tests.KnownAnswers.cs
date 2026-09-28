// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the Argon2 known-answer vectors and their runners. The partial is self-contained so that
/// <c>Bodu.Security.Cryptography.Simd.Test</c> can compile it on its own and hold the scalar fallback to the same
/// vectors.
/// </summary>
public partial class Argon2Tests
{
    /// <summary>The resource name of the reference implementation's <c>src/test.c</c>.</summary>
    private const string ReferenceTestSourceResourceName = "Bodu.Security.Cryptography.Argon2.test.c";

    /// <summary>The resource names of the reference implementation's version 0x10 trace files.</summary>
    private static readonly string[] ReferenceTraceResourceNames =
    [
        "Bodu.Security.Cryptography.Argon2.argon2d_v16",
        "Bodu.Security.Cryptography.Argon2.argon2i_v16",
        "Bodu.Security.Cryptography.Argon2.argon2id_v16",
    ];

    /// <summary>The resource name of the corpus recorded from the published 1.0.0 package.</summary>
    private const string RecordedCorpusResourceName = "Bodu.Security.Cryptography.Argon2.argon2-1.0.0-corpus.txt";

    /// <summary>The memory, in KiB, from which a reference vector belongs to the Stress tier (the 1 GiB rows).</summary>
    private const int LargeRamMemoryKiB = 1 << 20;

    /// <summary>The citation recorded for the reference implementation's vectors.</summary>
    private const string ReferenceImplementationCitation = "phc-winner-argon2 at f57e61e";

    /// <summary>
    /// Gets the reference implementation's vectors below 1 GiB: every <c>hashtest</c> call in <c>src/test.c</c>
    /// (Argon2i at versions 0x10 and 0x13, Argon2id at 0x13), and the version 0x10 traces for all three variants.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> ReferenceImplementationVectors() =>
        ReadReferenceHashTests()
            .Where(vector => vector.Memory < LargeRamMemoryKiB)
            .Concat(ReadReferenceTraces())
            .Select(vector => new object[] { vector });

    /// <summary>
    /// Gets the reference implementation's 1 GiB vectors, which <c>src/test.c</c> guards with <c>TEST_LARGE_RAM</c>.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> ReferenceImplementationLargeRamVectors() =>
        ReadReferenceHashTests()
            .Where(vector => vector.Memory >= LargeRamMemoryKiB)
            .Select(vector => new object[] { vector });

    /// <summary>
    /// Gets the reference implementation's vectors below 1 GiB that publish a PHC encoding alongside the tag.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" /> with its encoding.</returns>
    public static IEnumerable<object[]> ReferenceImplementationEncodings() =>
        ReadReferenceHashTests()
            .Where(vector => vector.Memory < LargeRamMemoryKiB && vector.Encoded is not null)
            .Select(vector => new object[] { vector });

    /// <summary>
    /// Gets the corpus recorded from the published 1.0.0 package: every variant at both versions, across the shapes
    /// the external vectors do not reach.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> RecordedCorpusVectors()
    {
        using Stream stream = OpenResource(RecordedCorpusResourceName);

        foreach (KdfKnownAnswer vector in Argon2RecordedCorpusReader.Read(stream, "Bodu 1.0.0", "Bodu.Security.Cryptography 1.0.0 (nuget.org)").ToArray())
            yield return [vector];
    }

    /// <summary>
    /// Verifies that each variant reproduces the reference implementation's tag for every vector below 1 GiB, at both
    /// versions.
    /// </summary>
    /// <param name="vector">The reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ReferenceImplementationVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveKey_WhenGivenReferenceImplementationVector_ShouldMatchExpectedTag(KdfKnownAnswer vector)
    {
        byte[] tag = DeriveKnownAnswer(vector);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that Argon2i reproduces the reference implementation's 1 GiB tags at both versions.
    /// </summary>
    /// <param name="vector">The reference vector under test.</param>
    [TestMethod]
    [TestCategory("Stress")]
    [DynamicData(nameof(ReferenceImplementationLargeRamVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveKey_WhenGivenReferenceImplementationLargeRamVector_ShouldMatchExpectedTag(KdfKnownAnswer vector)
    {
        byte[] tag = DeriveKnownAnswer(vector);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that a PHC string produced by the reference implementation verifies against its password, including the
    /// version 0x10 strings that carry no <c>v=</c> field.
    /// </summary>
    /// <param name="vector">The reference vector whose encoding is verified.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ReferenceImplementationEncodings), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Verify_WhenGivenReferenceImplementationEncoding_ShouldReturnTrue(KdfKnownAnswer vector)
    {
        bool verified = Argon2.Verify(vector.Encoded!, vector.Password);

        Assert.IsTrue(verified, vector.Encoded);
    }

    /// <summary>
    /// Verifies that every variant derives the tag the published 1.0.0 package recorded, bit for bit.
    /// </summary>
    /// <param name="vector">The recorded vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(RecordedCorpusVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveKey_WhenGivenRecordedCorpusVector_ShouldMatchRecordedTag(KdfKnownAnswer vector)
    {
        byte[] tag = DeriveKnownAnswer(vector);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant());
    }

    /// <summary>
    /// Reads every <c>hashtest</c> call from the embedded reference <c>src/test.c</c>.
    /// </summary>
    /// <returns>The vectors, in source order.</returns>
    private static KdfKnownAnswer[] ReadReferenceHashTests()
    {
        using Stream stream = OpenResource(ReferenceTestSourceResourceName);

        return [.. Argon2ReferenceKatReader.ReadHashTests(stream, $"{ReferenceImplementationCitation} src/test.c")];
    }

    /// <summary>
    /// Reads the embedded reference version 0x10 trace files.
    /// </summary>
    /// <returns>One vector per trace file.</returns>
    private static IEnumerable<KdfKnownAnswer> ReadReferenceTraces()
    {
        foreach (string resourceName in ReferenceTraceResourceNames)
        {
            using Stream stream = OpenResource(resourceName);

            yield return Argon2ReferenceKatReader.ReadTrace(stream, $"{ReferenceImplementationCitation} kats/{resourceName[(resourceName.LastIndexOf('.') + 1)..]}");
        }
    }

    /// <summary>
    /// Opens an embedded vector file of this assembly.
    /// </summary>
    /// <param name="resourceName">The resource's logical name.</param>
    /// <returns>A readable stream over the resource.</returns>
    /// <exception cref="InvalidOperationException">The resource is missing.</exception>
    private static Stream OpenResource(string resourceName) =>
        typeof(Argon2Tests).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' is missing.");

    /// <summary>
    /// Gets the RFC 9106, Section 5 test vectors for Argon2d, Argon2i, and Argon2id. All three share the same inputs and
    /// cost parameters and differ only in the resulting tag.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> Rfc9106Vectors()
    {
        byte[] password = Filled(0x01, 32);
        byte[] salt = Filled(0x02, 16);
        byte[] secret = Filled(0x03, 8);
        byte[] associatedData = Filled(0x04, 12);

        yield return [new KdfKnownAnswer
        {
            Name = "RFC 9106 Section 5.1 (Argon2d)",
            Provenance = KatProvenance.Rfc("RFC 9106 Section 5.1"),
            Variant = "d",
            Password = password, Salt = salt, Secret = secret, AssociatedData = associatedData,
            Memory = 32, Iterations = 3, Parallelism = 4, Version = 0x13, OutputLength = 32,
            ExpectedHex = "512b391b6f1162975371d30919734294f868e3be3984f3c1a13a4db9fabe4acb",
        }];
        yield return [new KdfKnownAnswer
        {
            Name = "RFC 9106 Section 5.2 (Argon2i)",
            Provenance = KatProvenance.Rfc("RFC 9106 Section 5.2"),
            Variant = "i",
            Password = password, Salt = salt, Secret = secret, AssociatedData = associatedData,
            Memory = 32, Iterations = 3, Parallelism = 4, Version = 0x13, OutputLength = 32,
            ExpectedHex = "c814d9d1dc7f37aa13f0d77f2494bda1c8de6b016dd388d29952a4c4672b6ce8",
        }];
        yield return [new KdfKnownAnswer
        {
            Name = "RFC 9106 Section 5.3 (Argon2id)",
            Provenance = KatProvenance.Rfc("RFC 9106 Section 5.3"),
            Variant = "id",
            Password = password, Salt = salt, Secret = secret, AssociatedData = associatedData,
            Memory = 32, Iterations = 3, Parallelism = 4, Version = 0x13, OutputLength = 32,
            ExpectedHex = "0d640df58d78766c08c037a34a8b53c9d01ef0452d75b65eb52520e96b01e659",
        }];
    }

    /// <summary>
    /// Verifies that each Argon2 variant reproduces its RFC 9106, Section 5 reference tag exactly.
    /// </summary>
    /// <param name="vector">The reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Rfc9106Vectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveKey_WhenGivenRfc9106Vector_ShouldMatchExpectedTag(KdfKnownAnswer vector)
    {
        byte[] tag = DeriveKnownAnswer(vector);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant());
    }

    /// <summary>
    /// Derives the tag a known-answer vector describes, through the public one-shot of the variant it names.
    /// </summary>
    /// <param name="vector">The vector whose inputs and cost parameters are derived.</param>
    /// <returns>The derived tag.</returns>
    private static byte[] DeriveKnownAnswer(KdfKnownAnswer vector)
    {
        var parameters = new Argon2Parameters
        {
            MemoryKiB = vector.Memory,
            Iterations = vector.Iterations,
            Parallelism = vector.Parallelism,
            TagLength = vector.OutputLength,
            Version = vector.Version,
            Secret = vector.Secret,
            AssociatedData = vector.AssociatedData,
        };

        return vector.Variant switch
        {
            "d" => Argon2d.DeriveKey(vector.Password, vector.Salt, parameters),
            "i" => Argon2i.DeriveKey(vector.Password, vector.Salt, parameters),
            _ => Argon2id.DeriveKey(vector.Password, vector.Salt, parameters),
        };
    }

    /// <summary>
    /// Creates a byte array of <paramref name="count" /> copies of <paramref name="value" />.
    /// </summary>
    /// <param name="value">The byte to repeat.</param>
    /// <param name="count">The number of bytes.</param>
    /// <returns>The filled array.</returns>
    private static byte[] Filled(byte value, int count)
    {
        byte[] result = new byte[count];
        Array.Fill(result, value);
        return result;
    }
}
