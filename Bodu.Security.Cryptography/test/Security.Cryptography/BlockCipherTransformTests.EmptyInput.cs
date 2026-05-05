// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.EmptyInput.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="BlockCipherTransform.TransformFinalBlock(byte[], int, int)" /> for the
/// well-known empty-input edge case. The current implementation guards
/// <see cref="BlockCipherTransform.TransformFinalBlock(byte[], int, int)" /> with
/// <c>CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize)</c> with the
/// default <c>throwIfZero == true</c>, so calling <c>TransformFinalBlock(buffer, 0, 0)</c> raises
/// <see cref="CryptographicException" /> — which propagates out of the BCL's
/// <see cref="CryptoStream.FlushFinalBlock" /> path, which always invokes
/// <c>TransformFinalBlock</c> at end-of-stream (even when nothing was written). That makes
/// encrypting an empty plaintext under <see cref="PaddingMode.PKCS7" /> fail with a misleading
/// "block length must be a positive multiple" message, contradicting the standard expectation
/// that a PKCS7-padded empty input produces exactly one block of all-pad bytes.
/// </summary>
[TestClass]
public sealed class BlockCipherTransformTests_EmptyInput
{
    /// <summary>
    /// Verifies that encrypting an empty plaintext with <see cref="PaddingMode.PKCS7" /> produces
    /// exactly one block of ciphertext (the padded all-pad block) and round-trips back to an
    /// empty plaintext on decrypt — the documented PKCS7 contract.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenInputIsEmptyAndPaddingIsPkcs7_ShouldProduceOneBlockAndRoundTrip_fix()
    {
        using var algorithm = new Skipjack
        {
            Padding = PaddingMode.PKCS7,
            Mode = CipherMode.CBC,
        };
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        byte[] cipherText = algorithm.Encrypt(System.ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(algorithm.BlockSize / 8, cipherText.Length,
            "PKCS7-padded empty input should produce exactly one block of ciphertext.");

        byte[] recovered = algorithm.Decrypt(cipherText);

        Assert.AreEqual(0, recovered.Length,
            "Decrypting one PKCS7-padded block of empty plaintext should produce an empty array.");
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.TransformFinalBlock(byte[], int, int)" />
    /// invoked directly with <c>inputCount == 0</c> on a Skipjack PKCS7 encryptor produces a
    /// single block of padded ciphertext rather than throwing. The framework's
    /// <see cref="CryptoStream.FlushFinalBlock" /> always invokes this path with whatever residual
    /// data sits in its buffer — including zero bytes — so rejecting zero-length input breaks
    /// every encrypt/decrypt pipeline that flows through <see cref="CryptoStream" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputCountIsZeroAndPaddingIsPkcs7_ShouldProduceOnePaddedBlock_fix()
    {
        using var algorithm = new Skipjack
        {
            Padding = PaddingMode.PKCS7,
            Mode = CipherMode.CBC,
        };
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform transform = algorithm.CreateEncryptor();
        byte[] result = transform.TransformFinalBlock(System.Array.Empty<byte>(), 0, 0);

        Assert.AreEqual(algorithm.BlockSize / 8, result.Length,
            "TransformFinalBlock with zero-length input under PKCS7 should produce one block of padded ciphertext.");
    }

    /// <summary>
    /// Verifies that <see cref="CryptoStream" /> can flush a freshly-opened
    /// <see cref="ICryptoTransform" /> without writing any data first — the
    /// <see cref="CryptoStream.FlushFinalBlock" /> path always invokes
    /// <c>TransformFinalBlock</c> with whatever sits in its internal buffer.
    /// Regression guard for the latent <c>throwIfZero == true</c> bug in
    /// <see cref="BlockCipherTransform" />.
    /// </summary>
    [TestMethod]
    public void CryptoStream_WhenFlushedWithoutWritingAnyData_ShouldProduceOnePkcs7PaddedBlock_fix()
    {
        using var algorithm = new Skipjack
        {
            Padding = PaddingMode.PKCS7,
            Mode = CipherMode.CBC,
        };
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform encryptor = algorithm.CreateEncryptor();
        using var output = new MemoryStream();
        using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
        {
            // Do not write any data; FlushFinalBlock should still produce one PKCS7 padding block.
            crypto.FlushFinalBlock();
        }

        Assert.AreEqual(algorithm.BlockSize / 8, output.Length,
            "Flushing a CryptoStream without writing any data under PKCS7 should still emit one block of padding.");
    }
}
