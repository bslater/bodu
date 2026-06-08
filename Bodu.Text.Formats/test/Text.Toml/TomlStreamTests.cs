// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for the <see cref="Toml" /> stream surface — UTF-8 parsing and formatting, the asynchronous
/// overloads, byte-order-mark handling, and argument validation.
/// </summary>
[TestClass]
public sealed class TomlStreamTests
{
    private const string Sample = "title = \"x\"\n\n[server]\nhost = \"localhost\"\nport = 8080\n";

    [TestMethod]
    public void FormatThenParse_WhenUsingStreams_ShouldRoundTrip()
    {
        var document = Toml.Parse(Sample);

        using MemoryStream stream = new();
        Toml.Format(document, stream);
        stream.Position = 0;
        var reparsed = Toml.Parse(stream);

        Assert.AreEqual(Normalize(document), Normalize(reparsed));
    }

    [TestMethod]
    public async Task FormatAsyncThenParseAsync_WhenUsingStreams_ShouldRoundTrip()
    {
        var document = Toml.Parse(Sample);

        using MemoryStream stream = new();
        await Toml.FormatAsync(document, stream);
        stream.Position = 0;
        var reparsed = await Toml.ParseAsync(stream);

        Assert.AreEqual(Normalize(document), Normalize(reparsed));
    }

    [TestMethod]
    public void Format_WhenWritingToStream_ShouldNotEmitByteOrderMark()
    {
        using MemoryStream stream = new();
        Toml.Format(new TomlTable { { "a", new TomlInteger(1) } }, stream);

        var bytes = stream.ToArray();
        Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.AreEqual("a = 1\n", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [TestMethod]
    public void Parse_WhenStreamHasByteOrderMark_ShouldIgnoreIt()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(System.Text.Encoding.UTF8.GetBytes("a = 1")).ToArray();

        using MemoryStream stream = new(bytes);
        var document = Toml.Parse(stream);

        Assert.AreEqual(1, ((TomlInteger)document["a"]).Value);
    }

    [TestMethod]
    public void Parse_WhenStreamHasNonAsciiUtf8_ShouldDecodeCorrectly()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("name = \"José\"\nemoji = \"\U0001F600\""));
        var document = Toml.Parse(stream);

        Assert.AreEqual("José", ((TomlString)document["name"]).Value);
        Assert.AreEqual("\U0001F600", ((TomlString)document["emoji"]).Value);
    }

    [TestMethod]
    public void Parse_WhenNullStream_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Toml.Parse((Stream)null!));
    }

    [TestMethod]
    public void Format_WhenNullArguments_ShouldThrowExactly()
    {
        using MemoryStream stream = new();
        Assert.ThrowsExactly<ArgumentNullException>(() => Toml.Format(null!, stream));
        Assert.ThrowsExactly<ArgumentNullException>(() => Toml.Format(new TomlTable(), (Stream)null!));
    }

    [TestMethod]
    public void Format_WhenStreamNotWritable_ShouldThrowExactly()
    {
        using MemoryStream stream = new(new byte[8], writable: false);

        Assert.ThrowsExactly<ArgumentException>(() => Toml.Format(new TomlTable { { "a", new TomlInteger(1) } }, stream));
    }

    [TestMethod]
    public void Parse_WhenStreamContentsInvalid_ShouldThrowTomlFormatException()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("a = "));

        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(stream));
    }

    private static string Normalize(TomlValue value)
    {
        switch (value)
        {
            case TomlTable table:
                var entries = new List<string>();
                foreach (var pair in table)
                    entries.Add("<" + pair.Key + ">=" + Normalize(pair.Value));
                entries.Sort(StringComparer.Ordinal);
                return "{" + string.Join(",", entries) + "}";
            case TomlArray array:
                var items = new List<string>();
                foreach (var item in array)
                    items.Add(Normalize(item));
                return "[" + string.Join(",", items) + "]";
            default:
                return value.Kind + ":" + value;
        }
    }
}
