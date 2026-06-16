// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Covers the <see cref="Argon2" /> destination-length guard, random-salt hash, type-and-data PHC encoding, and the
/// PHC decode failures surfaced by <see cref="Argon2.Verify(string, ReadOnlySpan{byte})" />.
/// </summary>
[TestClass]
public sealed class Argon2CoverageTests
{
    private static readonly byte[] Password = System.Text.Encoding.ASCII.GetBytes("password");
    private static readonly byte[] Salt = System.Text.Encoding.ASCII.GetBytes("saltsalt");

    private static Argon2Parameters FastParameters() =>
        new() { MemoryKiB = 64, Iterations = 1, Parallelism = 1 };

    /// <summary>
    /// Verifies that deriving into a destination whose length differs from the tag length is rejected.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenDestinationLengthWrong_ShouldThrowArgumentException()
    {
        var argon = new Argon2id(FastParameters());

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = stackalloc byte[16];
            argon.DeriveKey(Password, Salt, destination);
        });
    }

    /// <summary>
    /// Verifies that the random-salt hash overload produces an Argon2id PHC encoded-hash string.
    /// </summary>
    [TestMethod]
    public void Hash_WithRandomSalt_ShouldProduceEncodedString()
    {
        string encoded = new Argon2id(FastParameters()).Hash(Password);

        Assert.IsTrue(encoded.StartsWith("$argon2id$", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the Argon2d and Argon2i variants encode their type in the PHC string and round-trip through verify.
    /// </summary>
    /// <param name="argon2d">Whether to exercise Argon2d (otherwise Argon2i).</param>
    /// <param name="expectedTag">The expected PHC type tag.</param>
    [TestMethod]
    [DataRow(true, "$argon2d$")]
    [DataRow(false, "$argon2i$")]
    public void Hash_ForVariant_ShouldEncodeTypeAndVerify(bool argon2d, string expectedTag)
    {
        Argon2 argon = argon2d ? new Argon2d(FastParameters()) : new Argon2i(FastParameters());

        string encoded = argon.Hash(Password, Salt);

        Assert.IsTrue(encoded.StartsWith(expectedTag, StringComparison.Ordinal));
        Assert.IsTrue(Argon2.Verify(encoded, Password));
    }

    /// <summary>
    /// Verifies that a PHC string whose algorithm field is not a recognized Argon2 variant is rejected.
    /// </summary>
    [TestMethod]
    public void Verify_WhenAlgorithmUnrecognized_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = Argon2.Verify("$notargon2$v=19$m=64,t=1,p=1$c2FsdHNhbHQ$aGFzaA", Password));
    }
}
