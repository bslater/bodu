// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

[TestClass]
public sealed class IniReaderTests
{
    private const string Sample =
        "; leading comment\n" +
        "global = 1\n" +
        "\n" +
        "[database]\n" +
        "host = localhost\n" +
        "port = 5432\n" +
        "[empty]\n" +
        "flag\n";

    /// <summary>
    /// Verifies that <see cref="IniReader.Read" /> returns <see langword="false" /> immediately on empty input.
    /// </summary>
    [TestMethod]
    public void Read_WhenInputIsEmpty_ShouldReturnFalse()
    {
        using IniReader reader = new(new StringReader(string.Empty));

        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="IniReader.Read" /> surfaces each key/value entry in source order with its section,
    /// skipping comments and blank lines, and treats a key-only line as an empty value.
    /// </summary>
    [TestMethod]
    public void Read_WhenSourceHasSectionsAndComments_ShouldYieldEntriesInOrder()
    {
        using IniReader reader = new(new StringReader(Sample));

        List<(string Section, string Key, string Value)> rows = new();
        while (reader.Read())
            rows.Add((reader.Section, reader.Key, reader.Value));

        CollectionAssert.AreEqual(
            new[]
            {
                (string.Empty, "global", "1"),
                ("database", "host", "localhost"),
                ("database", "port", "5432"),
                ("empty", "flag", string.Empty),
            },
            rows);
    }

    /// <summary>
    /// Verifies that the streaming reader yields the same flat (section, key, value) sequence that enumerating the
    /// document produced by <see cref="Ini.Parse(System.ReadOnlySpan{char})" /> produces for input without duplicates.
    /// </summary>
    [TestMethod]
    public void Read_ShouldMatchParseDocumentEnumeration()
    {
        IniDocument document = Ini.Parse(Sample);

        List<(string, string, string)> fromParse = new();
        foreach (IniEntry e in document.GlobalSection.Entries)
            fromParse.Add((string.Empty, e.Key, e.Value));
        foreach (IniSection s in document.Sections)
            foreach (IniEntry e in s.Entries)
                fromParse.Add((s.Name, e.Key, e.Value));

        using IniReader reader = new(new StringReader(Sample));
        List<(string, string, string)> fromReader = new();
        while (reader.Read())
            fromReader.Add((reader.Section, reader.Key, reader.Value));

        CollectionAssert.AreEqual(fromParse, fromReader);
    }

    /// <summary>
    /// Verifies that <see cref="IniReader.ReadAsync" /> surfaces the same entries as the synchronous
    /// <see cref="IniReader.Read" /> path.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_ShouldYieldSameEntriesAsRead()
    {
        using IniReader reader = new(new StringReader(Sample));

        List<(string, string, string)> rows = new();
        while (await reader.ReadAsync())
            rows.Add((reader.Section, reader.Key, reader.Value));

        Assert.HasCount(4, rows);
        Assert.AreEqual(("database", "host", "localhost"), rows[1]);
    }

    /// <summary>
    /// Verifies that a malformed section header (missing closing bracket) throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSectionHeaderMalformed_ShouldThrowExactly()
    {
        using IniReader reader = new(new StringReader("[unterminated\n"));

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a global key appearing while <see cref="IniParseOptions.AllowGlobalSection" /> is
    /// <see langword="false" /> throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenGlobalKeyDisallowed_ShouldThrowExactly()
    {
        using IniReader reader = new(
            new StringReader("orphan = 1\n"),
            new IniParseOptions { AllowGlobalSection = false });

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> reader.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenReaderIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new IniReader(null!);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="IniReader.Read" /> after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenDisposed_ShouldThrowExactly()
    {
        IniReader reader = new(new StringReader(Sample));
        reader.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that an empty section header (<c>[]</c>) throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSectionHeaderIsEmpty_ShouldThrowIniFormatException()
    {
        using IniReader reader = new(new StringReader("[]"));

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a key/value line with an empty key throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenKeyIsEmpty_ShouldThrowIniFormatException()
    {
        using IniReader reader = new(new StringReader("[s]\n=value"));

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniReader.LineNumber" /> reflects the source line of the most recent entry.
    /// </summary>
    [TestMethod]
    public void LineNumber_AfterReadingEntry_ShouldReflectSourceLine()
    {
        using IniReader reader = new(new StringReader("[s]\nk=v"));

        _ = reader.Read();

        Assert.AreEqual(2, reader.LineNumber);
    }

    /// <summary>
    /// Verifies that disposing an <see cref="IniReader" /> twice is a no-op on the second call.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        IniReader reader = new(new StringReader("k=v"));

        reader.Dispose();
        reader.Dispose();
    }
}
