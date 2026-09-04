// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageReaderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMessageReaderOptions" />, the reader options.
/// </summary>
[TestClass]
public class OutlookMessageReaderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults mirror the container defaults.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldMirrorContainerDefaults()
    {
        var options = new OutlookMessageReaderOptions();

        Assert.AreEqual(CompoundValidationLevel.Compatible, options.ValidationLevel);
        Assert.AreEqual(CompoundReadStrategy.Buffered, options.ReadStrategy);
        Assert.IsTrue(options.DecompressRtf);
    }

    /// <summary>
    /// Verifies that initialized values are preserved.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialized_ShouldPreserveValues()
    {
        var options = new OutlookMessageReaderOptions
        {
            ValidationLevel = CompoundValidationLevel.Strict,
            ReadStrategy = CompoundReadStrategy.Streaming,
            DecompressRtf = false,
        };

        Assert.AreEqual(CompoundValidationLevel.Strict, options.ValidationLevel);
        Assert.AreEqual(CompoundReadStrategy.Streaming, options.ReadStrategy);
        Assert.IsFalse(options.DecompressRtf);
    }

    /// <summary>
    /// Verifies that the resource limits default to the documented values and preserve initialized values.
    /// </summary>
    [TestMethod]
    public void Limits_WhenDefaultedOrInitialized_ShouldCarryDocumentedValues()
    {
        var defaults = new OutlookMessageReaderOptions();
        Assert.AreEqual(16, defaults.MaxEmbeddedMessageDepth);
        Assert.AreEqual(64 * 1024 * 1024, defaults.MaxDecompressedRtfBytes);
        Assert.AreEqual(1024 * 1024, defaults.MaxInlineAttachmentBytes);

        var options = new OutlookMessageReaderOptions { MaxEmbeddedMessageDepth = 2, MaxDecompressedRtfBytes = 1024, MaxInlineAttachmentBytes = 512 };
        Assert.AreEqual(2, options.MaxEmbeddedMessageDepth);
        Assert.AreEqual(1024, options.MaxDecompressedRtfBytes);
        Assert.AreEqual(512, options.MaxInlineAttachmentBytes);
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
            _ = new OutlookMessageReaderOptions { MaxEmbeddedMessageDepth = value };
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OutlookMessageReaderOptions { MaxDecompressedRtfBytes = value };
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OutlookMessageReaderOptions { MaxInlineAttachmentBytes = value };
        });
    }
}
