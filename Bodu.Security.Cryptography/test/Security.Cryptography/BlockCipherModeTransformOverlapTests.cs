// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeTransformOverlapTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// End-to-end wire-up tests confirming that every <see cref="IBlockCipherModeTransform" /> implementation invokes
/// <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> and rejects partial input/output overlap. The detailed
/// helper semantics are covered separately in <c>CryptoHelpersTests.ThrowIfInvalidOverlap</c>.
/// </summary>
[TestClass]
public sealed class BlockCipherModeTransformOverlapTests
{
    private const int BlockSizeBytes = 8;

    private static IBlockCipher CreateCipher() => new SkipjackBlockCipher(new byte[10]);

    private static byte[] CreateIv() => new byte[BlockSizeBytes];

    /// <summary>
    /// Verifies that <see cref="CbcModeTransform.Transform" /> rejects a partial input/output overlap with
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CbcModeTransform_Transform_WhenPartialOverlap_ShouldThrowCryptographicException()
    {
        using var mode = new CbcModeTransform(CreateCipher(), CreateIv());

        var buffer = new byte[BlockSizeBytes * 4];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = mode.Transform(buffer.AsSpan(0, BlockSizeBytes * 2), buffer.AsSpan(BlockSizeBytes, BlockSizeBytes * 2), encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EcbModeTransform.Transform" /> rejects a partial input/output overlap with
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void EcbModeTransform_Transform_WhenPartialOverlap_ShouldThrowCryptographicException()
    {
        using var mode = new EcbModeTransform(CreateCipher());

        var buffer = new byte[BlockSizeBytes * 4];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = mode.Transform(buffer.AsSpan(0, BlockSizeBytes * 2), buffer.AsSpan(BlockSizeBytes, BlockSizeBytes * 2), encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" /> rejects a partial input/output overlap with
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CtrModeTransform_Transform_WhenPartialOverlap_ShouldThrowCryptographicException()
    {
        using var mode = new CtrModeTransform(CreateCipher(), CreateIv());

        var buffer = new byte[BlockSizeBytes * 4];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = mode.Transform(buffer.AsSpan(0, BlockSizeBytes * 2), buffer.AsSpan(BlockSizeBytes, BlockSizeBytes * 2), encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CfbModeTransform.Transform" /> rejects a partial input/output overlap with
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CfbModeTransform_Transform_WhenPartialOverlap_ShouldThrowCryptographicException()
    {
        using var mode = new CfbModeTransform(CreateCipher(), CreateIv());

        var buffer = new byte[BlockSizeBytes * 4];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = mode.Transform(buffer.AsSpan(0, BlockSizeBytes * 2), buffer.AsSpan(BlockSizeBytes, BlockSizeBytes * 2), encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OfbModeTransform.Transform" /> rejects a partial input/output overlap with
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void OfbModeTransform_Transform_WhenPartialOverlap_ShouldThrowCryptographicException()
    {
        using var mode = new OfbModeTransform(CreateCipher(), CreateIv());

        var buffer = new byte[BlockSizeBytes * 4];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = mode.Transform(buffer.AsSpan(0, BlockSizeBytes * 2), buffer.AsSpan(BlockSizeBytes, BlockSizeBytes * 2), encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CtsModeTransform.Transform" /> rejects a partial input/output overlap with
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CtsModeTransform_Transform_WhenPartialOverlap_ShouldThrowCryptographicException()
    {
        using var mode = new CtsModeTransform(CreateCipher(), CreateIv());

        var buffer = new byte[BlockSizeBytes * 4];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = mode.Transform(buffer.AsSpan(0, BlockSizeBytes * 2), buffer.AsSpan(BlockSizeBytes, BlockSizeBytes * 2), encrypt: true);
        });
    }
}
