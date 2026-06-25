// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini;

[TestClass]
public sealed class IniWriterTests
{
    /// <summary>
    /// Verifies that <see cref="IniWriter" /> emits section headers and key/value entries in the same shape produced
    /// by <see cref="Ini.Format(IniDocument)" />, such that the output re-parses to an equivalent document.
    /// </summary>
    [TestMethod]
    public void Write_ShouldRoundTripThroughParse()
    {
        StringWriter sw = new();
        using (IniWriter writer = new(sw))
        {
            writer.WriteSection("database");
            writer.WriteEntry("host", "localhost");
            writer.WriteEntry("port", "5432");
        }

        IniDocument document = Ini.Parse(sw.ToString());

        Assert.HasCount(1, document.Sections);
        IniSection db = document.GetSection("database")!;
        Assert.AreEqual("localhost", db["host"]);
        Assert.AreEqual("5432", db["port"]);
    }

    /// <summary>
    /// Verifies that output written by <see cref="IniWriter" /> is consumed by <see cref="IniReader" /> as the same
    /// section/key/value entries that were written.
    /// </summary>
    [TestMethod]
    public void Write_ShouldRoundTripThroughIniReader()
    {
        StringWriter sw = new();
        using (IniWriter writer = new(sw))
        {
            writer.WriteSection("server");
            writer.WriteEntry("name", "alpha");
        }

        using IniReader reader = new(new StringReader(sw.ToString()));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("server", reader.Section);
        Assert.AreEqual("name", reader.Key);
        Assert.AreEqual("alpha", reader.Value);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="IniWriter.WriteComment" /> emits a comment line with the supplied prefix.
    /// </summary>
    [TestMethod]
    public void WriteComment_ShouldEmitPrefixedLine()
    {
        StringWriter sw = new();
        using (IniWriter writer = new(sw))
        {
            writer.WriteComment("a note", '#');
        }

        Assert.AreEqual("#a note\n", sw.ToString());
    }

    /// <summary>
    /// Verifies that the asynchronous write methods produce the same output as their synchronous counterparts.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_ShouldMatchSyncOutput()
    {
        StringBuilder sb = new();
        using (StringWriter sw = new(sb))
        using (IniWriter writer = new(sw))
        {
            await writer.WriteSectionAsync("s");
            await writer.WriteEntryAsync("k", "v");
        }

        Assert.AreEqual("[s]\nk = v\n", sb.ToString());
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> writer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWriterIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new IniWriter(null!);
        });
    }

    /// <summary>
    /// Verifies that writing after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void WriteEntry_WhenDisposed_ShouldThrowExactly()
    {
        IniWriter writer = new(new StringWriter());
        writer.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            writer.WriteEntry("k", "v");
        });
    }

    /// <summary>
    /// Verifies that disposing an <see cref="IniWriter" /> twice is a no-op on the second call.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        IniWriter writer = new(new StringWriter());

        writer.Dispose();
        writer.Dispose();
    }
}
