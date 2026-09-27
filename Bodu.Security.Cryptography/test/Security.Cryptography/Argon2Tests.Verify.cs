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
