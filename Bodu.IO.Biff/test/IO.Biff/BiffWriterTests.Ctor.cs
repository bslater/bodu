// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Test.Assertions;

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a writer reports its version, maximum payload length, and an empty stream.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expectedMax">The expected maximum payload.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 2080)]
    [DataRow(BiffVersion.Biff8, 8224)]
    public void Ctor_WhenVersionSupported_ShouldConfigureLimits(BiffVersion version, int expectedMax)
    {
        var writer = new BiffWriter(new ArrayBufferWriter<byte>(), version);

        Assert.AreEqual(version, writer.Version);
        Assert.AreEqual(expectedMax, writer.MaxPayloadLength);
        Assert.AreEqual(0L, writer.BytesCommitted);
        Assert.AreEqual(0, writer.OpenSubstreamDepth);
        Assert.AreEqual(BiffLimits.DefaultCodePage, writer.CodePage);
    }

    /// <summary>
    /// Verifies that an unknown version is rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenVersionUnknown_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => _ = new BiffWriter(new ArrayBufferWriter<byte>(), BiffVersion.Unknown),
            "options");
    }

    /// <summary>
    /// Verifies that a null destination is rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOutputIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () => _ = new BiffWriter(null!, BiffVersion.Biff8),
            "output");
    }

    /// <summary>
    /// Verifies that the options code page is normalized and reported.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsCodePage_ShouldNormalize()
    {
        var writer = new BiffWriter(new ArrayBufferWriter<byte>(), new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 0x8000 });

        Assert.AreEqual(10000, writer.CodePage);
    }

    /// <summary>
    /// Verifies that a by-value copy of the writer continues the same stream.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCopiedByValue_ShouldShareStreamState()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        BiffWriter copy = writer;

        copy.WriteBof(BiffSubstreamType.Worksheet);

        Assert.AreEqual(20L, writer.BytesCommitted);
        Assert.AreEqual(1, writer.OpenSubstreamDepth);
    }
}
