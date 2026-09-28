// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.Verify.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
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
    /// Verifies that a password verifies against its PHC string whatever bound is placed on the derivation's threads,
    /// including bounds that divide the lanes among threads and one that confines them to the calling thread.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound on the derivation's threads.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(64)]
    public void Verify_WhenBoundIsGiven_ShouldReturnTrueForTheMatchingPassword(int maxDegreeOfParallelism)
    {
        byte[] secret = Repeat(0x09, 16);
        byte[] password = Encoding.UTF8.GetBytes("hunter2");
        string encoded = Argon2id.Hash(password, Repeat(0x07, 16), ThreadedParameters() with { Secret = secret });

        Assert.IsTrue(Argon2.Verify(encoded, password, secret, maxDegreeOfParallelism));
    }

    /// <summary>
    /// Verifies that a wrong password does not verify when a bound is placed on the derivation's threads.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound on the derivation's threads.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    [DataRow(4)]
    public void Verify_WhenBoundIsGivenAndPasswordDoesNotMatch_ShouldReturnFalse(int maxDegreeOfParallelism)
    {
        string encoded = Argon2id.Hash(Encoding.UTF8.GetBytes("hunter2"), Repeat(0x07, 16), ThreadedParameters());

        Assert.IsFalse(Argon2.Verify(encoded, Encoding.UTF8.GetBytes("hunter3"), [], maxDegreeOfParallelism));
    }

    /// <summary>
    /// Verifies that the bounded overload dispatches on the variant named in the string, as the unbounded one does.
    /// </summary>
    /// <param name="variant">The variant that produced the string.</param>
    [TestMethod]
    [DataRow("d")]
    [DataRow("i")]
    [DataRow("id")]
    public void Verify_WhenBoundIsGiven_ShouldVerifyEveryVariant(string variant)
    {
        byte[] password = Encoding.UTF8.GetBytes("hunter2");
        string encoded = Create(variant, FastParameters(), 1).Hash(password, Repeat(0x07, 16));

        Assert.IsTrue(Argon2.Verify(encoded, password, [], 2));
    }

    /// <summary>
    /// Verifies that the bounded overload rejects a bound of zero or below <c>-1</c>, naming the parameter, before it
    /// reads the string.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The invalid bound.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    [DataRow(int.MinValue)]
    public void Verify_WhenBoundIsInvalid_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Argon2.Verify("not-a-phc-string", "password"u8, [], maxDegreeOfParallelism);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the bounded overload rejects a null string, naming the parameter.
    /// </summary>
    [TestMethod]
    public void Verify_WhenBoundIsGivenAndEncodedIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Argon2.Verify(null!, "password"u8, [], 1);
        });

        Assert.AreEqual("encoded", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a PHC string whose algorithm field is not a recognized Argon2 variant is rejected.
    /// </summary>
    [TestMethod]
    public void Verify_WhenAlgorithmUnrecognized_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Argon2.Verify("$notargon2$v=19$m=64,t=1,p=1$c2FsdHNhbHQ$aGFzaA", "password"u8);
        });
    }
}
