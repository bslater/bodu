// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreReaderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMailStoreReaderOptions" />, the mail-store reader options.
/// </summary>
[TestClass]
public class OutlookMailStoreReaderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults mirror the container defaults and carry the documented resource limits.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldCarryDocumentedDefaults()
    {
        var options = new OutlookMailStoreReaderOptions();

        Assert.AreEqual(PstValidationLevel.Compatible, options.ValidationLevel);
        Assert.AreEqual(256, options.BlockCacheSize);
        Assert.IsTrue(options.DecompressRtf);
        Assert.AreEqual(256L * 1024 * 1024, options.MaxNodeDataLength);
        Assert.AreEqual(16, options.MaxEmbeddedMessageDepth);
        Assert.AreEqual(64 * 1024 * 1024, options.MaxDecompressedRtfBytes);
        Assert.AreEqual(1024 * 1024, options.MaxInlineAttachmentBytes);
    }

    /// <summary>
    /// Verifies that initialized values are preserved and that the container-facing limits flow through to the
    /// container options.
    /// </summary>
    [TestMethod]
    public void ToPstFileOptions_WhenInitialized_ShouldPassLimitsThrough()
    {
        var options = new OutlookMailStoreReaderOptions
        {
            ValidationLevel = PstValidationLevel.Strict,
            BlockCacheSize = 8,
            MaxNodeDataLength = 4096,
            MaxEmbeddedMessageDepth = 2,
            MaxDecompressedRtfBytes = 1024,
        };

        PstFileOptions container = options.ToPstFileOptions();

        Assert.AreEqual(PstValidationLevel.Strict, container.ValidationLevel);
        Assert.AreEqual(8, container.BlockCacheSize);
        Assert.AreEqual(4096L, container.MaxNodeDataLength);
        Assert.AreEqual(2, options.MaxEmbeddedMessageDepth);
        Assert.AreEqual(1024, options.MaxDecompressedRtfBytes);
    }

    /// <summary>
    /// Verifies that a zero or negative limit throws <see cref="ArgumentOutOfRangeException" /> for each limit.
    /// </summary>
    /// <param name="value">The rejected value.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Limits_WhenNotPositive_ShouldThrowArgumentOutOfRangeException(int value)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OutlookMailStoreReaderOptions { MaxNodeDataLength = value };
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OutlookMailStoreReaderOptions { MaxEmbeddedMessageDepth = value };
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OutlookMailStoreReaderOptions { MaxDecompressedRtfBytes = value };
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OutlookMailStoreReaderOptions { MaxInlineAttachmentBytes = value };
        });
    }
}
