// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfOutputBufferTooSmall.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfOutputBufferTooSmall(Span{byte}, int, string)"/> does not throw
    /// when the output buffer length matches the required size.
    /// </summary>
    [TestMethod]
    public void ThrowIfOutputBufferTooSmall_WhenBufferIsExactSize_ShouldNotThrow()
    {
        Span<byte> buffer = stackalloc byte[8];
        CryptoHelpers.ThrowIfOutputBufferTooSmall(buffer, 8);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfOutputBufferTooSmall(Span{byte}, int, string)"/> does not throw
    /// when the output buffer is larger than required.
    /// </summary>
    [TestMethod]
    public void ThrowIfOutputBufferTooSmall_WhenBufferLargerThanRequired_ShouldNotThrow()
    {
        Span<byte> buffer = stackalloc byte[16];
        CryptoHelpers.ThrowIfOutputBufferTooSmall(buffer, 8);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfOutputBufferTooSmall(Span{byte}, int, string)"/> throws an
    /// <see cref="ArgumentException"/> when the output buffer is smaller than required.
    /// </summary>
    [TestMethod]
    public void ThrowIfOutputBufferTooSmall_WhenBufferTooSmall_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> buffer = stackalloc byte[4];
            CryptoHelpers.ThrowIfOutputBufferTooSmall(buffer, 8);
        });

        Assert.AreEqual("buffer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfOutputBufferTooSmall(Span{byte}, int, string)"/> includes the
    /// required size in the exception message.
    /// </summary>
    [TestMethod]
    public void ThrowIfOutputBufferTooSmall_WhenBufferTooSmall_ShouldIncludeRequiredSizeInMessage()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> buffer = stackalloc byte[4];
            CryptoHelpers.ThrowIfOutputBufferTooSmall(buffer, 16);
        });

        Assert.IsTrue(ex.Message.Contains("16", StringComparison.Ordinal));
    }
}
