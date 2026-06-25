// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pkcs7PaddingTests.SymmetricAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// PKCS#7 round-trip and empty-input regression tests driven across every concrete
/// <see cref="SymmetricAlgorithm" /> declared in the library. The latent
/// <c>throwIfZero == true</c> bug in <see cref="BlockCipherTransform" /> manifested as a
/// misleading "block length must be a positive multiple" <see cref="CryptographicException" />
/// when <see cref="CryptoStream.FlushFinalBlock" /> ran <c>TransformFinalBlock(_, 0, 0)</c> on
/// an empty plaintext under <see cref="PaddingMode.PKCS7" /> — a path the BCL always reaches
/// at end-of-stream. Each test below configures the algorithm with PKCS7 over CBC and asserts
/// the documented contract: an empty plaintext yields exactly one block of all-pad ciphertext
/// that round-trips back to an empty array, partial blocks are padded out, and intermediate
/// state survives chunked writes through <see cref="CryptoStream" />.
/// </summary>
public sealed partial class Pkcs7PaddingTests
{
    /// <summary>
    /// Verifies that encrypting an empty plaintext under <see cref="PaddingMode.PKCS7" />
    /// produces exactly one block of ciphertext (the all-pad block) and round-trips back to an
    /// empty plaintext on decrypt — the canonical PKCS#7 contract.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void Encrypt_WhenInputIsEmpty_ShouldProduceOneBlockAndRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        int blockBytes = algorithm.BlockSize / 8;

        byte[] cipherText = algorithm.Encrypt([]);

        Assert.HasCount(blockBytes, cipherText,
            $"PKCS7-padded empty input on {algorithmType.Name} should produce exactly one block of ciphertext.");

        byte[] recovered = algorithm.Decrypt(cipherText);

        Assert.IsEmpty(recovered,
            $"Decrypting one PKCS7-padded block of empty plaintext on {algorithmType.Name} should produce an empty array.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" />
    /// invoked with <c>inputCount == 0</c> on a PKCS7 encryptor produces a single block of
    /// padded ciphertext rather than throwing — the framework's
    /// <see cref="CryptoStream.FlushFinalBlock" /> always invokes this path with whatever
    /// residual data sits in its buffer, including zero bytes.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void TransformFinalBlock_WhenInputCountIsZero_ShouldProduceOnePaddedBlock(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        int blockBytes = algorithm.BlockSize / 8;

        using ICryptoTransform transform = algorithm.CreateEncryptor();
        byte[] result = transform.TransformFinalBlock([], 0, 0);

        Assert.HasCount(blockBytes, result,
            $"TransformFinalBlock with zero-length input under PKCS7 on {algorithmType.Name} " +
            "should produce one block of padded ciphertext.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />
    /// invoked with <c>inputCount == 0</c> returns <c>0</c> rather than raising
    /// <see cref="CryptographicException" /> — the
    /// <see cref="ICryptoTransform" /> contract treats a zero-byte <c>TransformBlock</c> as a
    /// no-op, and <see cref="CryptoStream" /> can reach this path after a flush even when no
    /// further data has been written.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void TransformBlock_WhenInputCountIsZero_ShouldReturnZero(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        int blockBytes = algorithm.BlockSize / 8;

        using ICryptoTransform transform = algorithm.CreateEncryptor();
        byte[] outputBuffer = new byte[blockBytes];

        int written = transform.TransformBlock([], 0, 0, outputBuffer, 0);

        Assert.AreEqual(0, written,
            $"TransformBlock with inputCount == 0 on {algorithmType.Name} must return 0 rather than throw.");
    }

    /// <summary>
    /// Verifies that flushing a freshly-opened <see cref="CryptoStream" /> without writing
    /// any data still emits one PKCS#7 padding block — <see cref="CryptoStream.FlushFinalBlock" />
    /// always invokes <c>TransformFinalBlock</c> with whatever residual sits in its buffer, so
    /// rejecting zero-length input would break every encrypt pipeline that flushes early.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenFlushedWithoutWritingAnyData_ShouldProduceOnePaddedBlock(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        int blockBytes = algorithm.BlockSize / 8;

        using ICryptoTransform encryptor = algorithm.CreateEncryptor();
        using var output = new MemoryStream();
        long writtenLength;
        using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            crypto.FlushFinalBlock();
        }

        writtenLength = output.Length;

        Assert.AreEqual(blockBytes, writtenLength,
            $"Flushing a CryptoStream without writing any data under PKCS7 on {algorithmType.Name} " +
            "should still emit one block of padding.");
    }

    /// <summary>
    /// Verifies that a block-aligned plaintext round-trips through <see cref="CryptoStream" />
    /// under <see cref="PaddingMode.PKCS7" /> — the cipher writes the plaintext blocks and
    /// appends a full block of padding (since the input is already aligned), and the decrypt
    /// pipeline strips that padding back off.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenBlockAlignedPlaintext_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes * 2).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.HasCount(plaintext.Length + blockBytes, cipherText,
            $"Block-aligned plaintext under PKCS7 on {algorithmType.Name} should produce ciphertext one block longer than the plaintext.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Block-aligned plaintext under PKCS7 on {algorithmType.Name} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that a sub-block plaintext round-trips through <see cref="CryptoStream" /> under
    /// <see cref="PaddingMode.PKCS7" /> — PKCS#7 pads the trailing residual out to the next
    /// block boundary, and the decrypt pipeline recovers the original prefix.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenResidualPlaintext_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes + 3).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.AreEqual(0, cipherText.Length % blockBytes,
            $"PKCS7 ciphertext on {algorithmType.Name} must be block-aligned.");
        Assert.IsGreaterThan(plaintext.Length, cipherText.Length,
            $"Residual plaintext under PKCS7 on {algorithmType.Name} should grow to the next block boundary.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Residual plaintext under PKCS7 on {algorithmType.Name} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that a multi-write <see cref="CryptoStream" /> session — calling
    /// <see cref="Stream.Write(byte[], int, int)" /> several times with chunks of varying sizes
    /// that do not align with the cipher's block boundary — still produces ciphertext that
    /// decrypts to the concatenated plaintext under <see cref="PaddingMode.PKCS7" />. This
    /// drives the residual-buffering paths inside
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> that a
    /// single-write test cannot reach.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenWritingInUnalignedChunks_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.PKCS7);
        byte[] plaintext = Enumerable.Range(0, 41).Select(i => (byte)(i * 7 + 11)).ToArray();
        int[] chunkSizes = new[] { 1, 3, 5, 7, 11, 13 };

        byte[] cipherText;
        using (var output = new MemoryStream())
        {
            using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
            {
                int offset = 0;
                int chunkIndex = 0;
                while (offset < plaintext.Length)
                {
                    int len = System.Math.Min(chunkSizes[chunkIndex % chunkSizes.Length], plaintext.Length - offset);
                    crypto.Write(plaintext, offset, len);
                    offset += len;
                    chunkIndex++;
                }
            }

            cipherText = output.ToArray();
        }

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Chunked write under PKCS7 on {algorithmType.Name} did not round-trip through CryptoStream.");
    }

}
