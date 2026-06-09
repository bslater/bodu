// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Covers the associated-data bridge on <see cref="IAeadTransform" /> and the empty-message S2V padding path of
/// <see cref="SivModeTransform" />.
/// </summary>
[TestClass]
public sealed class SivModeTransformCoverageTests
{
    private static readonly byte[] S2vKey = Convert.FromHexString("fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0");
    private static readonly byte[] CtrKey = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");

    private static SivModeTransform CreateTransform() =>
        new(
            s2vCipher: new AesBlockCipherFixture(S2vKey),
            ctrCipher: new AesBlockCipherFixture(CtrKey),
            iv: new byte[16]);

    /// <summary>
    /// Verifies that encrypting and decrypting an empty plaintext through the associated-data bridge round-trips,
    /// exercising both the bridge and the empty-message S2V padding.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_ViaAssociatedDataBridge_WithEmptyPlaintext_ShouldRoundTrip()
    {
        byte[] associatedData = { 1, 2, 3 };
        byte[] plaintext = Array.Empty<byte>();
        var output = new byte[16];

        int written;
        using (SivModeTransform encryptor = CreateTransform())
            written = ((IAeadTransform)encryptor).Encrypt(plaintext, output, associatedData);

        var recovered = Array.Empty<byte>();
        int decrypted;
        using (SivModeTransform decryptor = CreateTransform())
            decrypted = ((IAeadTransform)decryptor).Decrypt(output.AsSpan(0, written), recovered, associatedData);

        Assert.AreEqual((16, 0), (written, decrypted));
    }
}
