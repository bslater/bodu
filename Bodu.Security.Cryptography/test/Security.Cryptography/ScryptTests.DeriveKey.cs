// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptTests.DeriveKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ScryptTests
{
    /// <summary>
    /// Verifies that the <see cref="SecretBytes" /> / <see cref="Salt" /> typed overload of
    /// <see cref="Scrypt.DeriveKey(SecretBytes, Salt, int, int, int, int)" /> derives the same key as the span-based
    /// overload for the same password and salt bytes.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenGivenTypedPasswordAndSalt_ShouldMatchSpanOverload()
    {
        byte[] passwordBytes = "correct horse battery staple"u8.ToArray();
        byte[] saltBytes = "NaCl"u8.ToArray();

        byte[] expected = Scrypt.DeriveKey(passwordBytes, saltBytes, costN: 16, blockSizeR: 1, parallelization: 1, length: 32);

        using var password = SecretBytes.CopyFrom(passwordBytes);
        byte[] actual = Scrypt.DeriveKey(password, Salt.FromBytes(saltBytes), costN: 16, blockSizeR: 1, parallelization: 1, length: 32);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the typed overload of <see cref="Scrypt.DeriveKey(SecretBytes, Salt, int, int, int, int)" />
    /// throws <see cref="ArgumentNullException" /> when the password holder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenTypedPasswordIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Scrypt.DeriveKey((SecretBytes)null!, Salt.FromBytes("NaCl"u8), costN: 16, blockSizeR: 1, parallelization: 1, length: 32);
        });

        Assert.AreEqual("password", ex.ParamName);
    }
}
