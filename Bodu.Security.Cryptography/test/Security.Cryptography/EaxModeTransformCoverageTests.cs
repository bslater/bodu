// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Covers the empty-message OMAC padding path of <see cref="EaxModeTransform" />.
/// </summary>
[TestClass]
public sealed class EaxModeTransformCoverageTests
{
    private static readonly byte[] Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");

    private static EaxModeTransform CreateTransform() =>
        new(new AesBlockCipherFixture(Key), new byte[16]);

    /// <summary>
    /// Verifies that encrypting and decrypting an empty plaintext round-trips, exercising the empty-message OMAC
    /// padding (<c>0x80 || 0…0</c>).
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithEmptyPlaintext_ShouldRoundTrip()
    {
        byte[] plaintext = Array.Empty<byte>();
        byte[] output = new byte[16];

        int written;
        using (EaxModeTransform encryptor = CreateTransform())
            written = encryptor.Encrypt(plaintext, output);

        byte[] recovered = Array.Empty<byte>();
        int decrypted;
        using (EaxModeTransform decryptor = CreateTransform())
            decrypted = decryptor.Decrypt(output.AsSpan(0, written), recovered);

        Assert.AreEqual((16, 0), (written, decrypted));
    }
}
