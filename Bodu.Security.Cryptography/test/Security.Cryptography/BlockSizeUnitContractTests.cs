// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockSizeUnitContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Library-wide contract tests for block-size units across the low-level <see cref="IBlockCipher" /> layer,
/// the BCL <see cref="SymmetricAlgorithm" /> adapter layer, and the bridging <see cref="BlockCipherTransform" />.
/// </summary>
/// <remarks>
/// The contract is intentional and load-bearing:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="IBlockCipher.BlockSize" /> reports the cipher's block size in <em>bits</em> (matches the BCL
/// <see cref="SymmetricAlgorithm.BlockSize" /> convention so the same number flows through both layers).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ICryptoTransform.InputBlockSize" /> and <see cref="ICryptoTransform.OutputBlockSize" /> report
/// the value in <em>bytes</em> (per the BCL contract), computed as <c>IBlockCipher.BlockSize / 8</c>.
/// </description>
/// </item>
/// </list>
/// These tests catch any future change that quietly flips a unit at one boundary while leaving the other in place.
/// </remarks>
[TestClass]
public sealed class BlockSizeUnitContractTests
{
    /// <summary>
    /// Verifies that every low-level <see cref="IBlockCipher" /> implementation reports its block size in bits,
    /// matches the expected value, and is a multiple of eight.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherFactories))]
    public void IBlockCipher_BlockSize_ShouldReportBitsAndBeMultipleOfEight(
        string name,
        Func<IBlockCipher> factory,
        int expectedBlockSizeBits)
    {
        using IBlockCipher cipher = factory();

        Assert.AreEqual(expectedBlockSizeBits, cipher.BlockSize, $"{name}: IBlockCipher.BlockSize must report bits.");
        Assert.AreEqual(0, cipher.BlockSize % 8, $"{name}: IBlockCipher.BlockSize must be a multiple of 8.");
        Assert.IsTrue(cipher.BlockSize >= 64, $"{name}: BlockSize implausibly small for a block cipher.");
    }

    /// <summary>
    /// Verifies that every <see cref="SymmetricAlgorithm" />-derived cipher reports its block size in bits and
    /// that the bridging <see cref="ICryptoTransform.InputBlockSize" /> / <see cref="ICryptoTransform.OutputBlockSize" />
    /// equal <c>BlockSize / 8</c> (bytes).
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmFactories))]
    public void SymmetricAlgorithm_BlockSizeAndTransform_ShouldAgreeOnBitsAndBytes(
        string name,
        Func<SymmetricAlgorithm> factory,
        int expectedBlockSizeBits)
    {
        using SymmetricAlgorithm alg = factory();

        Assert.AreEqual(expectedBlockSizeBits, alg.BlockSize, $"{name}: SymmetricAlgorithm.BlockSize must report bits.");

        using ICryptoTransform xfm = alg.CreateEncryptor();
        int expectedBytes = expectedBlockSizeBits / 8;

        Assert.AreEqual(expectedBytes, xfm.InputBlockSize, $"{name}: ICryptoTransform.InputBlockSize must equal BlockSize / 8.");
        Assert.AreEqual(expectedBytes, xfm.OutputBlockSize, $"{name}: ICryptoTransform.OutputBlockSize must equal BlockSize / 8.");
    }

    /// <summary>
    /// Returns factory delegates for every low-level <see cref="IBlockCipher" /> primitive together with the
    /// expected block size in bits.
    /// </summary>
    public static IEnumerable<object[]> BlockCipherFactories()
    {
        yield return new object[] { "AesBlockCipher", (Func<IBlockCipher>)(() => new AesBlockCipher(new byte[16])), 128 };
        yield return new object[] { "BlowfishBlockCipher", (Func<IBlockCipher>)(() => new BlowfishBlockCipher(new byte[16])), 64 };
        yield return new object[] { "SkipjackBlockCipher", (Func<IBlockCipher>)(() => new SkipjackBlockCipher(new byte[10])), 64 };
        yield return new object[] { "TwofishBlockCipher", (Func<IBlockCipher>)(() => new TwofishBlockCipher(new byte[16])), 128 };
        yield return new object[] { "CamelliaBlockCipher", (Func<IBlockCipher>)(() => new CamelliaBlockCipher(new byte[16])), 128 };
        yield return new object[] { "Serpent128Cipher", (Func<IBlockCipher>)(() => new Serpent128Cipher(new byte[16])), 128 };
        yield return new object[] { "Serpent256Cipher", (Func<IBlockCipher>)(() => new Serpent256Cipher(new byte[32], new byte[16])), 256 };
        yield return new object[] { "Serpent512Cipher", (Func<IBlockCipher>)(() => new Serpent512Cipher(new byte[64], new byte[16])), 512 };
        yield return new object[] { "Serpent1024Cipher", (Func<IBlockCipher>)(() => new Serpent1024Cipher(new byte[128], new byte[16])), 1024 };
        yield return new object[] { "Threefish256Cipher", (Func<IBlockCipher>)(() => new Threefish256Cipher(new byte[32], new byte[16])), 256 };
        yield return new object[] { "Threefish512Cipher", (Func<IBlockCipher>)(() => new Threefish512Cipher(new byte[64], new byte[16])), 512 };
        yield return new object[] { "Threefish1024Cipher", (Func<IBlockCipher>)(() => new Threefish1024Cipher(new byte[128], new byte[16])), 1024 };
    }

    /// <summary>
    /// Returns factory delegates for every <see cref="SymmetricAlgorithm" />-derived cipher together with the
    /// expected block size in bits.
    /// </summary>
    public static IEnumerable<object[]> SymmetricAlgorithmFactories()
    {
        yield return new object[] { "Skipjack", (Func<SymmetricAlgorithm>)(() => new Skipjack()), 64 };
        yield return new object[] { "Blowfish", (Func<SymmetricAlgorithm>)(() => new Blowfish()), 64 };
        yield return new object[] { "Camellia", (Func<SymmetricAlgorithm>)(() => new Camellia()), 128 };
        yield return new object[] { "Twofish", (Func<SymmetricAlgorithm>)(() => new Twofish()), 128 };
        yield return new object[] { "Serpent128", (Func<SymmetricAlgorithm>)(() => new Serpent128()), 128 };
    }
}
