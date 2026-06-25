// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the <see cref="Scrypt" /> implementation against the official RFC 7914 test vectors and exercises the PHC
/// encoded-hash round-trip, parameter validation, and the instance and static surfaces.
/// </summary>
[TestClass]
public class ScryptTests
{
    /// <summary>
    /// The three lighter RFC 7914, Section 12 test vectors (the fourth, ~1 GiB vector runs in the Stress tier).
    /// </summary>
    public static IEnumerable<object[]> Rfc7914Vectors()
    {
        yield return [new KdfKnownAnswerVector
        {
            Name = "RFC 7914 Section 12 (N=16, r=1, p=1)",
            Password = [], Salt = [], CostN = 16, BlockSizeR = 1, Parallelism = 1, OutputLength = 64,
            ExpectedHex = "77d6576238657b203b19ca42c18a0497f16b4844e3074ae8dfdffa3fede21442" +
                          "fcd0069ded0948f8326a753a0fc81f17e8d3e0fb2e0d3628cf35e20c38d18906",
        }];
        yield return [new KdfKnownAnswerVector
        {
            Name = "RFC 7914 Section 12 (password/NaCl, N=1024, r=8, p=16)",
            Password = Ascii("password"), Salt = Ascii("NaCl"), CostN = 1024, BlockSizeR = 8, Parallelism = 16, OutputLength = 64,
            ExpectedHex = "fdbabe1c9d3472007856e7190d01e9fe7c6ad7cbc8237830e77376634b373162" +
                          "2eaf30d92e22a3886ff109279d9830dac727afb94a83ee6d8360cbdfa2cc0640",
        }];
        yield return [new KdfKnownAnswerVector
        {
            Name = "RFC 7914 Section 12 (pleaseletmein/SodiumChloride, N=16384, r=8, p=1)",
            Password = Ascii("pleaseletmein"), Salt = Ascii("SodiumChloride"), CostN = 16384, BlockSizeR = 8, Parallelism = 1, OutputLength = 64,
            ExpectedHex = "7023bdcb3afd7348461c06cd81fd38ebfda8fbba904f8e3ea9b543f6545da1f2" +
                          "d5432955613f0fcf62d49705242a9af9e61e85dc0d651e40dfcf017b45575887",
        }];
    }

    /// <summary>
    /// Verifies that scrypt reproduces each lighter RFC 7914, Section 12 reference value exactly.
    /// </summary>
    /// <param name="vector">The reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Rfc7914Vectors), DynamicDataDisplayName = nameof(GetVectorName))]
    public void DeriveKey_WhenGivenRfc7914Vector_ShouldMatchExpectedOutput(KdfKnownAnswerVector vector)
    {
        byte[] derived = Scrypt.DeriveKey(vector.Password, vector.Salt, vector.CostN, vector.BlockSizeR, vector.Parallelism, vector.OutputLength);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(derived).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies the fourth RFC 7914, Section 12 vector, which requires roughly 1 GiB of memory.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void DeriveKey_WhenGivenRfc7914LargeMemoryVector_ShouldMatchExpectedOutput()
    {
        byte[] derived = Scrypt.DeriveKey(Ascii("pleaseletmein"), Ascii("SodiumChloride"), 1048576, 8, 1, 64);

        Assert.AreEqual(
            "2101cb9b6a511aaeaddbbe09cf70f881ec568d574a2ffd4dabe5ee9820adaa47" +
            "8e56fd8f4ba5d09ffa1c6d927c40f4c337304049e8a952fbcbf45c6fa77a41a4",
            Convert.ToHexString(derived).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that the instance and static one-shot surfaces produce identical output for the same inputs.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenInstanceAndStatic_ShouldProduceIdenticalOutput()
    {
        byte[] password = Ascii("correct horse battery staple");
        byte[] salt = Ascii("seasalt");

        byte[] instance = new Scrypt(16384, 8, 1).GetBytes(password, salt, 32);
        byte[] @static = Scrypt.DeriveKey(password, salt, 16384, 8, 1, 32);

        CollectionAssert.AreEqual(instance, @static);
    }

    /// <summary>
    /// Verifies that a password verifies against the PHC string produced for it, and a wrong password does not.
    /// </summary>
    [TestMethod]
    public void Verify_WhenPasswordMatchesEncodedHash_ShouldReturnTrue()
    {
        byte[] password = Ascii("hunter2");
        string encoded = Scrypt.Hash(password, Ascii("seasalt"), 16384, 8, 1, 32);

        Assert.IsTrue(Scrypt.Verify(encoded, password));
        Assert.IsFalse(Scrypt.Verify(encoded, Ascii("hunter3")));
    }

    /// <summary>
    /// Verifies that a constructor rejects a cost parameter <c>N</c> that is not a power of two.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCostNotPowerOfTwo_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Scrypt(1000, 8, 1);
        });

        Assert.AreEqual("CostN", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a constructor rejects a block size below 1.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBlockSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Scrypt(16384, 0, 1);
        });

        Assert.AreEqual("BlockSizeR", ex.ParamName);
    }

    /// <summary>
    /// Verifies that deriving with a zero-length output throws an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenLengthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var scrypt = new Scrypt(16384, 8, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = scrypt.GetBytes(Ascii("pw"), Ascii("salt"), 0);
        });
    }

    /// <summary>
    /// Verifies that scrypt derives output of the requested length on a happy-path input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DeriveKey_WhenGivenSimpleInput_ShouldReturnOutputOfRequestedLength()
    {
        byte[] derived = Scrypt.DeriveKey(Ascii("password"), Ascii("salt"), 1024, 8, 1, 32);

        Assert.HasCount(32, derived);
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
    /// Encodes an ASCII string to bytes.
    /// </summary>
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);
}
