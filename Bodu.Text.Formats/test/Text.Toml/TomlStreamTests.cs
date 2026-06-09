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

    /// <summary>
    /// Provides streams whose bytes are not valid UTF-8 and therefore must be rejected.
    /// </summary>
    /// <returns>One named byte sequence per row.</returns>
    public static IEnumerable<object[]> InvalidUtf8Cases()
    {
        yield return ["lone-continuation", new byte[] { 0x80 }];
        yield return ["overlong-slash", new byte[] { 0xC0, 0xAF }];
        yield return ["encoded-surrogate", new byte[] { 0xED, 0xA0, 0x80 }];
        yield return ["above-unicode-range", new byte[] { 0xF4, 0x90, 0x80, 0x80 }];
    }

    [TestMethod]
    public void FormatThenParse_WhenUsingStreams_ShouldRoundTrip()
    {
        TomlTable document = Toml.Parse(Sample);

        using MemoryStream stream = new();
        Toml.Format(document, stream);
        stream.Position = 0;
        TomlTable reparsed = Toml.Parse(stream);

        Assert.AreEqual(Normalize(document), Normalize(reparsed));
    }

    [TestMethod]
    public async Task FormatAsyncThenParseAsync_WhenUsingStreams_ShouldRoundTrip()
    {
        TomlTable document = Toml.Parse(Sample);

        using MemoryStream stream = new();
        await Toml.FormatAsync(document, stream);
        stream.Position = 0;
        TomlTable reparsed = await Toml.ParseAsync(stream);

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
        TomlTable document = Toml.Parse(stream);

        Assert.AreEqual(1, ((TomlInteger)document["a"]).Value);
    }

    /// <summary>
    /// Verifies that a second leading byte-order mark is rejected: the reader strips exactly one leading mark, so a
    /// doubled mark leaves an unexpected U+FEFF that fails parsing.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamHasDoubledByteOrderMark_ShouldThrowTomlFormatException()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var bytes = bom.Concat(bom).Concat(System.Text.Encoding.UTF8.GetBytes("a = 1")).ToArray();

        using MemoryStream stream = new(bytes);

        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(stream));
    }

    [TestMethod]
    public void Parse_WhenStreamHasNonAsciiUtf8_ShouldDecodeCorrectly()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("name = \"José\"\nemoji = \"\U0001F600\""));
        TomlTable document = Toml.Parse(stream);

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

    /// <summary>
    /// Verifies that a stream whose bytes are not valid UTF-8 is rejected with a <see cref="TomlFormatException" />
    /// rather than decoded with replacement characters.
    /// </summary>
    /// <param name="name">The scenario name, used as the failure message.</param>
    /// <param name="bytes">The invalid UTF-8 byte sequence.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidUtf8Cases))]
    public void Parse_WhenStreamHasInvalidUtf8_ShouldThrowTomlFormatException(string name, byte[] bytes)
    {
        using MemoryStream stream = new(bytes);

        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(stream), name);
    }

    /// <summary>
    /// Verifies that the asynchronous stream path also rejects bytes that are not valid UTF-8 with a
    /// <see cref="TomlFormatException" />, matching the synchronous path.
    /// </summary>
    /// <param name="name">The scenario name, used as the failure message.</param>
    /// <param name="bytes">The invalid UTF-8 byte sequence.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidUtf8Cases))]
    public async Task ParseAsync_WhenStreamHasInvalidUtf8_ShouldThrowTomlFormatException(string name, byte[] bytes)
    {
        using MemoryStream stream = new(bytes);

        await Assert.ThrowsExactlyAsync<TomlFormatException>(async () => await Toml.ParseAsync(stream), name);
    }

    /// <summary>
    /// Verifies that an already-cancelled token causes <see cref="Toml.ParseAsync(Stream, CancellationToken)" /> to
    /// observe cancellation.
    /// </summary>
    [TestMethod]
    public async Task ParseAsync_WhenCancellationAlreadyRequested_ShouldThrowOperationCanceledException()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("a = 1"));
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await Toml.ParseAsync(stream, cts.Token));
    }

    /// <summary>
    /// Verifies that an already-cancelled token causes
    /// <see cref="Toml.FormatAsync(TomlTable, Stream, CancellationToken)" /> to observe cancellation.
    /// </summary>
    [TestMethod]
    public async Task FormatAsync_WhenCancellationAlreadyRequested_ShouldThrowOperationCanceledException()
    {
        using MemoryStream stream = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await Toml.FormatAsync(new TomlTable { { "a", new TomlInteger(1) } }, stream, cts.Token));
    }

    /// <summary>
    /// Verifies that <see cref="Toml.FormatAsync(TomlTable, Stream, CancellationToken)" /> validates its arguments.
    /// </summary>
    [TestMethod]
    public async Task FormatAsync_WhenNullArguments_ShouldThrowExactly()
    {
        using MemoryStream stream = new();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await Toml.FormatAsync(null!, stream));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await Toml.FormatAsync(new TomlTable(), null!));
    }

    private static string Normalize(TomlValue value)
    {
        switch (value)
        {
            case TomlTable table:
                var entries = new List<string>();
                foreach (KeyValuePair<string, TomlValue> pair in table)
                    entries.Add("<" + pair.Key + ">=" + Normalize(pair.Value));
                entries.Sort(StringComparer.Ordinal);
                return "{" + string.Join(",", entries) + "}";
            case TomlArray array:
                var items = new List<string>();
                foreach (TomlValue item in array)
                    items.Add(Normalize(item));
                return "[" + string.Join(",", items) + "]";
            default:
                return value.Kind + ":" + value;
        }
    }
}
