// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly configured <typeparamref name="TAlgorithm" /> with a generated tweak
    /// round-trips a multi-block plaintext through the public
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.CreateEncryptor()" /> /
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.CreateDecryptor()" /> facade.
    /// </summary>
    /// <remarks>
    /// Complements the non-tweakable algorithm round-trip in
    /// <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" /> by exercising
    /// <see cref="TweakableSymmetricAlgorithm.GenerateTweak" /> and pinning the tweak's contribution to
    /// the encryptor / decryptor pairing.
    /// </remarks>
    [TestMethod]
    public void RoundTrip_WhenEncryptingThenDecryptingWithGeneratedTweak_ShouldRecoverPlaintext()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);
        algorithm.Padding = PaddingMode.PKCS7;
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();

        var plaintext = BuildIncrementingPlaintext(algorithm.BlockSize / 8 * 3);

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    private static byte[] BuildIncrementingPlaintext(int length)
    {
        var buffer = new byte[length];
        for (var i = 0; i < length; i++)
            buffer[i] = (byte)i;
        return buffer;
    }
}
