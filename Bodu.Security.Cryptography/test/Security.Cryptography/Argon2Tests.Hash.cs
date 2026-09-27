// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.Hash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
    /// <summary>
    /// Verifies that the random-salt hash overload produces an Argon2id PHC encoded-hash string.
    /// </summary>
    [TestMethod]
    public void Hash_WhenSaltIsGenerated_ShouldProduceArgon2idEncodedString()
    {
        string encoded = new Argon2id(FastParameters()).Hash("password"u8);

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
    public void Hash_WhenVariantIsArgon2dOrArgon2i_ShouldEncodeTypeAndVerify(bool argon2d, string expectedTag)
    {
        Argon2 argon = argon2d ? new Argon2d(FastParameters()) : new Argon2i(FastParameters());

        string encoded = argon.Hash("password"u8, "saltsalt"u8);

        Assert.IsTrue(encoded.StartsWith(expectedTag, StringComparison.Ordinal));
        Assert.IsTrue(Argon2.Verify(encoded, "password"u8));
    }
}
