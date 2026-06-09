// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for the <see cref="TomlReader" /> and <see cref="TomlWriter" /> types — the deserialization and
/// serialization halves of the read/write pair — including the inheritance surface that the configuration bridge
/// relies on.
/// </summary>
[TestClass]
public sealed class TomlReaderWriterTests
{
    [TestMethod]
    public void Reader_WhenReadString_ShouldDeserialize()
    {
        TomlTable document = new TomlReader().Read("a = 1\nb = \"x\"");

        Assert.AreEqual(1, ((TomlInteger)document["a"]).Value);
        Assert.AreEqual("x", ((TomlString)document["b"]).Value);
    }

    [TestMethod]
    public void Reader_WhenReadTextReader_ShouldDeserialize()
    {
        using var textReader = new StringReader("[t]\nx = 1");
        TomlTable document = new TomlReader().Read(textReader);

        Assert.AreEqual(1, ((TomlInteger)((TomlTable)document["t"])["x"]).Value);
    }

    [TestMethod]
    public void Reader_WhenTryReadValidAndInvalid_ShouldReflectOutcome()
    {
        var reader = new TomlReader();

        Assert.IsTrue(reader.TryRead("a = 1".AsSpan(), out TomlTable? document));
        Assert.AreEqual(1, ((TomlInteger)document["a"]).Value);
        Assert.IsFalse(reader.TryRead("a = ".AsSpan(), out TomlTable? failed));
        Assert.IsNull(failed);
    }

    [TestMethod]
    public void Reader_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TomlReader().Read((string)null!));
    }

    [TestMethod]
    public void Writer_WhenWriteString_ShouldSerialize()
    {
        var document = new TomlTable { { "a", new TomlInteger(1) } };

        Assert.AreEqual("a = 1\n", new TomlWriter().Write(document));
    }

    [TestMethod]
    public void Writer_WhenWriteTextWriter_ShouldSerialize()
    {
        using var stringWriter = new StringWriter();
        new TomlWriter().Write(new TomlTable { { "a", new TomlInteger(1) } }, stringWriter);

        Assert.AreEqual("a = 1\n", stringWriter.ToString());
    }

    [TestMethod]
    public void ReaderWriter_WhenRoundTripped_ShouldYieldEqualText()
    {
        const string Source = "title = \"x\"\n\n[server]\nhost = \"localhost\"\nport = 8080\n";
        var reader = new TomlReader();
        var writer = new TomlWriter();

        var once = writer.Write(reader.Read(Source));
        var twice = writer.Write(reader.Read(once));

        Assert.AreEqual(once, twice);
    }

    /// <summary>
    /// Verifies that a type inheriting <see cref="TomlReader" /> gains its read capability — the pattern the
    /// configuration bridge uses to consume TOML without a mutation surface.
    /// </summary>
    [TestMethod]
    public void Reader_WhenSubclassed_ShouldInheritReadCapability()
    {
        var reader = new DerivedReader();

        Assert.AreEqual(1, ((TomlInteger)reader.ReadDocument("a = 1")["a"]).Value);
    }

    private sealed class DerivedReader
        : TomlReader
    {
        public TomlTable ReadDocument(string source) => Read(source);
    }
}
