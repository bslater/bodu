// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv;

[TestClass]
public sealed class DotEnvWriterTests
{
    /// <summary>
    /// Verifies that values written by <see cref="DotEnvWriter" /> are quoted and escaped the same way as
    /// <see cref="DotEnv.Format(DotEnvDocument)" />, such that the output re-parses to the original key/value pairs.
    /// </summary>
    [TestMethod]
    public void Write_ShouldRoundTripThroughParse()
    {
        StringWriter sw = new();
        using (DotEnvWriter writer = new(sw))
        {
            writer.WriteEntry("HOST", "localhost");
            writer.WriteEntry("GREETING", "hello world");
            writer.WriteEntry("EMPTY", string.Empty);
            writer.WriteEntry("QUOTED", "has \"quote\" and\nnewline");
        }

        DotEnvDocument document = DotEnv.Parse(sw.ToString());

        Assert.AreEqual("localhost", document["HOST"]);
        Assert.AreEqual("hello world", document["GREETING"]);
        Assert.AreEqual(string.Empty, document["EMPTY"]);
        Assert.AreEqual("has \"quote\" and\nnewline", document["QUOTED"]);
    }

    /// <summary>
    /// Verifies that output written by <see cref="DotEnvWriter" /> is consumed by <see cref="DotEnvReader" /> as the
    /// same entries that were written, including a value that requires quoting.
    /// </summary>
    [TestMethod]
    public void Write_ShouldRoundTripThroughDotEnvReader()
    {
        StringWriter sw = new();
        using (DotEnvWriter writer = new(sw))
        {
            writer.WriteEntry("A", "1");
            writer.WriteEntry("B", "needs quoting # here");
        }

        using DotEnvReader reader = new(new StringReader(sw.ToString()));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(("A", "1"), (reader.Key, reader.Value));
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(("B", "needs quoting # here"), (reader.Key, reader.Value));
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that the asynchronous write path produces the same output as the synchronous one.
    /// </summary>
    [TestMethod]
    public async Task WriteEntryAsync_ShouldMatchSyncOutput()
    {
        StringBuilder sb = new();
        using (StringWriter sw = new(sb))
        using (DotEnvWriter writer = new(sw))
        {
            await writer.WriteEntryAsync("K", "v");
        }

        Assert.AreEqual("K=v\n", sb.ToString());
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> writer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWriterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DotEnvWriter(null!);
        });
    }

    /// <summary>
    /// Verifies that writing after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void WriteEntry_WhenDisposed_ShouldThrowExactly()
    {
        DotEnvWriter writer = new(new StringWriter());
        writer.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            writer.WriteEntry("k", "v");
        });
    }
}
