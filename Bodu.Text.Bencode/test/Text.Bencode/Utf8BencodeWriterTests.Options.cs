// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Utf8BencodeWriter" /> options behavior.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.Options" /> reports the effective configuration, including the
    /// resolved default depth and the root-value policy.
    /// </summary>
    [TestMethod]
    public void Options_WhenWriterConstructed_ShouldReportEffectiveConfiguration()
    {
        var buffer = new ArrayBufferWriter<byte>();

        var defaulted = new Utf8BencodeWriter(buffer);
        Assert.AreEqual(BencodeLimits.AbsoluteMaxDepth, defaulted.Options.MaxDepth);
        Assert.IsFalse(defaulted.Options.AllowMultipleRootValues);

        var configured = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 8, AllowMultipleRootValues = true });
        Assert.AreEqual(8, configured.Options.MaxDepth);
        Assert.IsTrue(configured.Options.AllowMultipleRootValues);
    }

}
