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
