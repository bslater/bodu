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
}
