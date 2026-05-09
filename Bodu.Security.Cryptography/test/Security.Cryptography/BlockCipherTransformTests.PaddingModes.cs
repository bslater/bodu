// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.PaddingModes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Drives <see cref="BlockCipherTransform" /> through the standard <see cref="CryptoStream" />
/// pipeline for every <see cref="PaddingMode" /> Skipjack supports. The earlier coverage in
/// <see cref="BlockCipherTransformTests_EmptyInput" /> only exercised <see cref="PaddingMode.PKCS7" />
/// — these tests pin the round-trip contract for <see cref="PaddingMode.None" />,
/// <see cref="PaddingMode.Zeros" />, <see cref="PaddingMode.ANSIX923" /> and
/// <see cref="PaddingMode.ISO10126" /> too, so a future refactor of the padding/finaliser path
/// cannot silently regress one mode while the others stay green.
/// </summary>
[TestClass]
public sealed class BlockCipherTransformTests_PaddingModes
{
    /// <summary>
    /// Verifies that an empty plaintext encrypted through a <see cref="CryptoStream" /> under
    /// <see cref="PaddingMode.ANSIX923" /> or <see cref="PaddingMode.ISO10126" /> produces exactly
    /// one block of ciphertext (the all-pad block) and round-trips back to an empty plaintext on
    /// decrypt — the documented contract for length-recoverable schemes.
    /// </summary>
    [DataRow(PaddingMode.ANSIX923)]
    [DataRow(PaddingMode.ISO10126)]
    [TestMethod]
    public void CryptoStream_WhenEmptyPlaintextAndLengthRecoverablePadding_ShouldEmitOnePaddedBlockAndRoundTrip(PaddingMode padding)
    {
        using var algorithm = CreateAlgorithm(padding);
        int blockBytes = algorithm.BlockSize / 8;

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, System.Array.Empty<byte>());

        Assert.AreEqual(blockBytes, cipherText.Length,
            $"Empty plaintext under {padding} should produce exactly one block of padded ciphertext.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, recovered.Length,
            $"Decrypting one {padding}-padded block of empty plaintext should recover an empty array.");
    }

    /// <summary>
    /// Verifies that an empty plaintext under <see cref="PaddingMode.None" /> or
    /// <see cref="PaddingMode.Zeros" /> produces an empty ciphertext through a
    /// <see cref="CryptoStream" /> — neither scheme adds any padding when the input is already
    /// at a block boundary, and zero bytes is trivially block-aligned.
    /// </summary>
    [DataRow(PaddingMode.None)]
    [DataRow(PaddingMode.Zeros)]
    [TestMethod]
    public void CryptoStream_WhenEmptyPlaintextAndNonRecoverablePadding_ShouldEmitEmptyCiphertext(PaddingMode padding)
    {
        using var algorithm = CreateAlgorithm(padding);

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, System.Array.Empty<byte>());

        Assert.AreEqual(0, cipherText.Length,
            $"Empty plaintext under {padding} should produce an empty ciphertext (no padding added).");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, recovered.Length,
            $"Decrypting empty ciphertext under {padding} should recover an empty array.");
    }

    /// <summary>
    /// Verifies that a block-aligned plaintext round-trips correctly through a
    /// <see cref="CryptoStream" /> for every padding mode. <see cref="PaddingMode.ANSIX923" /> and
    /// <see cref="PaddingMode.ISO10126" /> append a full block of padding (since the input is
    /// already aligned); the others leave the ciphertext at its original length.
    /// </summary>
    [DataRow(PaddingMode.None)]
    [DataRow(PaddingMode.Zeros)]
    [DataRow(PaddingMode.ANSIX923)]
    [DataRow(PaddingMode.ISO10126)]
    [TestMethod]
    public void CryptoStream_WhenBlockAlignedPlaintext_ShouldRoundTrip(PaddingMode padding)
    {
        using var algorithm = CreateAlgorithm(padding);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes * 2).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        bool addsFullPadBlockWhenAligned = padding is PaddingMode.ANSIX923 or PaddingMode.ISO10126 or PaddingMode.PKCS7;
        int expectedCipherLength = addsFullPadBlockWhenAligned
            ? plaintext.Length + blockBytes
            : plaintext.Length;

        Assert.AreEqual(expectedCipherLength, cipherText.Length,
            $"Block-aligned plaintext under {padding} produced unexpected ciphertext length.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Block-aligned plaintext under {padding} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that a sub-block plaintext round-trips correctly through a
    /// <see cref="CryptoStream" /> for the length-recoverable padding modes.
    /// <see cref="PaddingMode.None" /> rejects unaligned input and is excluded;
    /// <see cref="PaddingMode.Zeros" /> is non-recoverable for residual data and asserted
    /// separately to confirm the prefix is preserved even though Unpad cannot strip the trailing
    /// zero pad bytes.
    /// </summary>
    [DataRow(PaddingMode.ANSIX923)]
    [DataRow(PaddingMode.ISO10126)]
    [TestMethod]
    public void CryptoStream_WhenResidualPlaintextAndLengthRecoverablePadding_ShouldRoundTrip_fix(PaddingMode padding)
    {
        using var algorithm = CreateAlgorithm(padding);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes + 3).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.AreEqual(0, cipherText.Length % blockBytes,
            $"Ciphertext under {padding} must be block-aligned.");
        Assert.IsTrue(cipherText.Length > plaintext.Length,
            $"Residual plaintext under {padding} should have grown to the next block boundary.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Residual plaintext under {padding} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that <see cref="PaddingMode.Zeros" /> with a residual plaintext preserves the
    /// original prefix on decrypt, even though <see cref="ZeroPadding.Unpad" /> cannot strip the
    /// trailing zero bytes — they are indistinguishable from legitimate plaintext zeros.
    /// </summary>
    [TestMethod]
    public void CryptoStream_WhenResidualPlaintextAndZerosPadding_ShouldRecoverPrefixWithTrailingZeros_fix()
    {
        using var algorithm = CreateAlgorithm(PaddingMode.Zeros);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes + 3).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);
        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, cipherText.Length % blockBytes,
            "Zeros-padded ciphertext must be block-aligned.");
        Assert.IsTrue(recovered.Length >= plaintext.Length,
            "Zeros-padded recovered output must be at least as long as the original plaintext.");
        CollectionAssert.AreEqual(
            plaintext,
            recovered.Take(plaintext.Length).ToArray(),
            "Zeros-padded plaintext prefix must survive round-trip through CryptoStream.");
        Assert.IsTrue(
            recovered.Skip(plaintext.Length).All(b => b == 0),
            "Trailing bytes in Zeros-padded recovered output must all be zero.");
    }

    /// <summary>
    /// Verifies that a multi-write <see cref="CryptoStream" /> session — calling
    /// <see cref="System.IO.Stream.Write(byte[], int, int)" /> several times in chunks of varying
    /// sizes that do not align with the block boundary — still produces ciphertext that decrypts
    /// to the concatenated plaintext. This drives <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />
    /// through residual-buffering paths that a single-write test does not reach.
    /// </summary>
    [DataRow(PaddingMode.PKCS7)]
    [DataRow(PaddingMode.ANSIX923)]
    [DataRow(PaddingMode.ISO10126)]
    [TestMethod]
    public void CryptoStream_WhenWritingInUnalignedChunks_ShouldRoundTrip_fix(PaddingMode padding)
    {
        using var algorithm = CreateAlgorithm(padding);
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
            $"Chunked write under {padding} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Builds a Skipjack algorithm with the supplied padding mode and a fixed CBC configuration.
    /// </summary>
    private static Skipjack CreateAlgorithm(PaddingMode padding)
    {
        Skipjack algorithm = new Skipjack
        {
            Padding = padding,
            Mode = CipherMode.CBC,
        };
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return algorithm;
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> by writing it through a <see cref="CryptoStream" />
    /// wrapped around <paramref name="algorithm" />'s encryptor, returning the captured ciphertext.
    /// </summary>
    private static byte[] EncryptThroughCryptoStream(SymmetricAlgorithm algorithm, byte[] plaintext)
    {
        using var output = new MemoryStream();
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
        using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            if (plaintext.Length > 0)
                crypto.Write(plaintext, 0, plaintext.Length);

            crypto.FlushFinalBlock();
        }

        return output.ToArray();
    }

    /// <summary>
    /// Decrypts <paramref name="cipherText" /> by reading it through a <see cref="CryptoStream" />
    /// wrapped around <paramref name="algorithm" />'s decryptor, returning the recovered plaintext.
    /// </summary>
    private static byte[] DecryptThroughCryptoStream(SymmetricAlgorithm algorithm, byte[] cipherText)
    {
        using var source = new MemoryStream(cipherText);
        using ICryptoTransform decryptor = algorithm.CreateDecryptor();
        using var crypto = new CryptoStream(source, decryptor, CryptoStreamMode.Read);
        using var output = new MemoryStream();

        crypto.CopyTo(output);
        return output.ToArray();
    }
}
