// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the <see cref="Argon2d" />, <see cref="Argon2i" />, and <see cref="Argon2id" /> implementations against
/// the official RFC 9106 test vectors and exercises the PHC encoded-hash round-trip, parameter validation, and the
/// instance and static surfaces.
/// </summary>
[TestClass]
public class Argon2Tests
{
    /// <summary>
    /// The RFC 9106, Section 5 test vectors for Argon2d, Argon2i, and Argon2id. All three share the same inputs and
    /// cost parameters and differ only in the resulting tag.
    /// </summary>
    public static IEnumerable<object[]> Rfc9106Vectors()
    {
        byte[] password = Repeat(0x01, 32);
        byte[] salt = Repeat(0x02, 16);
        byte[] secret = Repeat(0x03, 8);
        byte[] associatedData = Repeat(0x04, 12);

        yield return [new KdfKnownAnswerVector
        {
            Name = "RFC 9106 Section 5.1 (Argon2d)",
            Variant = "d",
            Password = password, Salt = salt, Secret = secret, AssociatedData = associatedData,
            Memory = 32, Iterations = 3, Parallelism = 4, Version = 0x13, OutputLength = 32,
            ExpectedHex = "512b391b6f1162975371d30919734294f868e3be3984f3c1a13a4db9fabe4acb",
        }];
        yield return [new KdfKnownAnswerVector
        {
            Name = "RFC 9106 Section 5.2 (Argon2i)",
            Variant = "i",
            Password = password, Salt = salt, Secret = secret, AssociatedData = associatedData,
            Memory = 32, Iterations = 3, Parallelism = 4, Version = 0x13, OutputLength = 32,
            ExpectedHex = "c814d9d1dc7f37aa13f0d77f2494bda1c8de6b016dd388d29952a4c4672b6ce8",
        }];
        yield return [new KdfKnownAnswerVector
        {
            Name = "RFC 9106 Section 5.3 (Argon2id)",
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
    [DynamicData(nameof(Rfc9106Vectors), DynamicDataDisplayName = nameof(GetVectorName))]
    public void DeriveKey_WhenGivenRfc9106Vector_ShouldMatchExpectedTag(KdfKnownAnswerVector vector)
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

        byte[] tag = DeriveKey(vector.Variant, vector.Password, vector.Salt, parameters);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that the instance and static one-shot surfaces produce identical tags for the same inputs.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenInstanceAndStatic_ShouldProduceIdenticalTags()
    {
        var parameters = new Argon2Parameters { MemoryKiB = 64, Iterations = 2, Parallelism = 2 };
        byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");
        byte[] salt = Repeat(0x05, 16);

        byte[] instance = new Argon2id(parameters).GetBytes(password, salt);
        byte[] @static = Argon2id.DeriveKey(password, salt, parameters);

        CollectionAssert.AreEqual(instance, @static);
    }

    /// <summary>
    /// Verifies that a password verifies against the PHC string produced for it.
    /// </summary>
    [TestMethod]
    public void Verify_WhenPasswordMatchesEncodedHash_ShouldReturnTrue()
    {
        var parameters = new Argon2Parameters { MemoryKiB = 64, Iterations = 2, Parallelism = 1 };
        byte[] password = Encoding.UTF8.GetBytes("hunter2");
        byte[] salt = Repeat(0x07, 16);

        string encoded = Argon2id.Hash(password, salt, parameters);

        Assert.IsTrue(Argon2id.Verify(encoded, password));
        Assert.IsTrue(Argon2.Verify(encoded, password));
    }

    /// <summary>
    /// Verifies that an incorrect password does not verify against an encoded hash.
    /// </summary>
    [TestMethod]
    public void Verify_WhenPasswordDoesNotMatch_ShouldReturnFalse()
    {
        var parameters = new Argon2Parameters { MemoryKiB = 64, Iterations = 2, Parallelism = 1 };
        string encoded = Argon2id.Hash(Encoding.UTF8.GetBytes("hunter2"), Repeat(0x07, 16), parameters);

        Assert.IsFalse(Argon2id.Verify(encoded, Encoding.UTF8.GetBytes("hunter3")));
    }

    /// <summary>
    /// Verifies that the round trip preserves an Argon2 secret key (pepper).
    /// </summary>
    [TestMethod]
    public void Verify_WhenSecretSupplied_ShouldRoundTrip()
    {
        byte[] secret = Repeat(0x09, 16);
        var parameters = new Argon2Parameters { MemoryKiB = 64, Iterations = 2, Parallelism = 1, Secret = secret };
        byte[] password = Encoding.UTF8.GetBytes("p@ssword");
        string encoded = Argon2id.Hash(password, Repeat(0x07, 16), parameters);

        Assert.IsTrue(Argon2id.Verify(encoded, password, secret));
        Assert.IsFalse(Argon2id.Verify(encoded, password));
    }

    /// <summary>
    /// Verifies that a variant-specific verify rejects an encoded hash produced by a different variant.
    /// </summary>
    [TestMethod]
    public void Verify_WhenVariantDiffers_ShouldReturnFalse()
    {
        var parameters = new Argon2Parameters { MemoryKiB = 64, Iterations = 2, Parallelism = 1 };
        byte[] password = Encoding.UTF8.GetBytes("hunter2");
        string encoded = Argon2id.Hash(password, Repeat(0x07, 16), parameters);

        Assert.IsFalse(Argon2i.Verify(encoded, password));
        Assert.IsFalse(Argon2d.Verify(encoded, password));
    }

    /// <summary>
    /// Verifies that a constructor rejects a parallelism outside the permitted range.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenParallelismIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Argon2id(new Argon2Parameters { MemoryKiB = 64, Iterations = 1, Parallelism = 0 });
        });

        Assert.AreEqual("Parallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a constructor rejects memory below the 8 * parallelism floor.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenMemoryBelowFloor_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Argon2id(new Argon2Parameters { MemoryKiB = 4, Iterations = 1, Parallelism = 4 });
        });

        Assert.AreEqual("MemoryKiB", ex.ParamName);
    }

    /// <summary>
    /// Verifies that deriving with a salt shorter than 8 bytes throws an <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenSaltTooShort_ShouldThrowArgumentException()
    {
        var argon2 = new Argon2id(new Argon2Parameters { MemoryKiB = 64, Iterations = 1, Parallelism = 1 });

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = argon2.GetBytes(Encoding.UTF8.GetBytes("pw"), Repeat(0x01, 4));
        });

        Assert.AreEqual("salt", ex.ParamName);
    }

    /// <summary>
    /// Verifies that verifying a malformed PHC string throws a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Verify_WhenEncodedStringIsMalformed_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Argon2id.Verify("not-a-phc-string", Encoding.UTF8.GetBytes("pw"));
        });
    }

    /// <summary>
    /// Verifies that Argon2id derives a tag of the configured length on a happy-path input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DeriveKey_WhenGivenSimpleInput_ShouldReturnTagOfConfiguredLength()
    {
        byte[] tag = Argon2id.DeriveKey(
            Encoding.UTF8.GetBytes("password"),
            Repeat(0x02, 16),
            new Argon2Parameters { MemoryKiB = 64, Iterations = 1, Parallelism = 1, TagLength = 32 });

        Assert.AreEqual(32, tag.Length);
    }

    /// <summary>
    /// Produces a friendly display name for a <see cref="KdfKnownAnswerVector" /> data row.
    /// </summary>
    /// <param name="methodInfo">The test method being parameterized.</param>
    /// <param name="data">The data row.</param>
    /// <returns>The vector name, or the method name when unavailable.</returns>
    public static string GetVectorName(MethodInfo methodInfo, object[] data) =>
        data[0] is KdfKnownAnswerVector vector ? vector.Name : methodInfo.Name;

    /// <summary>
    /// Dispatches a derivation to the public type named by <paramref name="variant" />.
    /// </summary>
    private static byte[] DeriveKey(string variant, byte[] password, byte[] salt, Argon2Parameters parameters) => variant switch
    {
        "d" => Argon2d.DeriveKey(password, salt, parameters),
        "i" => Argon2i.DeriveKey(password, salt, parameters),
        _ => Argon2id.DeriveKey(password, salt, parameters),
    };

    /// <summary>
    /// Creates a byte array of <paramref name="count" /> copies of <paramref name="value" />.
    /// </summary>
    private static byte[] Repeat(byte value, int count)
    {
        byte[] result = new byte[count];
        Array.Fill(result, value);
        return result;
    }
}
