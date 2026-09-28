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

    /// <summary>
    /// Verifies that each variant's static one-shot hashes with a secret and associated data, that the associated
    /// data travels in the PHC string's <c>data=</c> field, and that the variant's static verify accepts the string
    /// with the secret and rejects it without.
    /// </summary>
    /// <param name="variant">The variant hashed.</param>
    [TestMethod]
    [DataRow("d")]
    [DataRow("i")]
    [DataRow("id")]
    public void Hash_WhenStaticWithSecretAndAssociatedData_ShouldVerifyWithTheSecret(string variant)
    {
        byte[] secret = Repeat(0x5C, 16);
        byte[] password = Repeat(0x70, 12);
        Argon2Parameters parameters = FastParameters() with { Secret = secret, AssociatedData = Repeat(0xAD, 6) };

        string encoded = variant switch
        {
            "d" => Argon2d.Hash(password, Repeat(0x51, 16), parameters),
            "i" => Argon2i.Hash(password, Repeat(0x51, 16), parameters),
            _ => Argon2id.Hash(password, Repeat(0x51, 16), parameters),
        };
        Func<string, byte[], byte[], bool> verify = variant switch
        {
            "d" => (value, candidate, key) => Argon2d.Verify(value, candidate, key),
            "i" => (value, candidate, key) => Argon2i.Verify(value, candidate, key),
            _ => (value, candidate, key) => Argon2id.Verify(value, candidate, key),
        };

        Assert.Contains(",data=", encoded);
        Assert.IsTrue(verify(encoded, password, secret));
        Assert.IsFalse(verify(encoded, password, []));
    }
}
