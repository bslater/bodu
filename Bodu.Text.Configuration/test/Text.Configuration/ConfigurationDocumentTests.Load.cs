// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.Load.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;
using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

public partial class ConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Load(string)" /> reads and parses a file from disk.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathExists_ShouldProduceDocument()
    {
        using TempFileScope scope = new(ConfigurationFixtures.Representative);

        var doc = ConfigurationDocument.Load(scope.Path);

        Assert.HasCount(2, doc.Sections);
        Assert.AreEqual("true", doc.GlobalSection["root"]);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Load(string)" /> rejects a <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ConfigurationDocument.Load((string)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Load(string)" /> rejects a whitespace path with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathIsWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ConfigurationDocument.Load("   ");
        });
    }

    /// <summary>
    /// Verifies that loading from a stream produces the same document as loading from text.
    /// </summary>
    [TestMethod]
    public void Load_WhenStreamProvided_ShouldProduceSameDocumentAsParse()
    {
        var fromText = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(ConfigurationFixtures.Representative));
        var fromStream = ConfigurationDocument.Load(stream);

        Assert.HasCount(fromText.Sections.Count, fromStream.Sections);
        Assert.HasCount(fromText.GlobalSection.Entries.Count, fromStream.GlobalSection.Entries);
    }

    /// <summary>
    /// Verifies that loading from a stream that does not support reading throws an
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenStreamCannotRead_ShouldThrowExactly()
    {
        using MemoryStream stream = new();
        stream.Close();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ConfigurationDocument.Load(stream);
        });
    }

    /// <summary>
    /// Verifies that loading from a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenStreamIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ConfigurationDocument.Load((Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that loading from a <see langword="null" /> <see cref="TextReader" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenTextReaderIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ConfigurationDocument.Load((TextReader)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.LineNumber" /> reflects the source line on a loaded document.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathProvided_ShouldAttachLineNumberToEntries()
    {
        using TempFileScope scope = new(ConfigurationFixtures.Minimal);

        var doc = ConfigurationDocument.Load(scope.Path);

        Assert.AreEqual(2, doc.Sections[0].Entries[0].LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Load(TextReader, ConfigurationParseOptions?)" /> ignores a byte
    /// order mark the reader delivers as the first character, as loading a stream ignores the stream's encoded mark.
    /// </summary>
    [TestMethod]
    public void Load_WhenTextReaderStartsWithByteOrderMark_ShouldIgnoreTheMark()
    {
        using var reader = new StringReader("\uFEFF[*]\nkey = value\n");

        var doc = ConfigurationDocument.Load(reader);

        Assert.AreEqual("*", doc.Sections[0].Name);
        Assert.AreEqual("value", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that a stream holding a UTF-8 byte order mark loads into the same document that
    /// <see cref="ConfigurationDocument.Parse(string)" /> reads from the decoded text, mark included.
    /// </summary>
    [TestMethod]
    public void Load_WhenStreamStartsWithUtf8ByteOrderMark_ShouldMatchParseOfTheDecodedText()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("[*]\nkey = value\n")];
        using var stream = new MemoryStream(bytes);

        var loaded = ConfigurationDocument.Load(stream);
        var parsed = ConfigurationDocument.Parse(Encoding.UTF8.GetString(bytes));

        Assert.AreEqual(loaded.Sections[0].Name, parsed.Sections[0].Name);
        Assert.AreEqual(loaded.Sections[0].Entries[0].Value, parsed.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that a document loaded from a path resolves its sections against the file's directory when no
    /// <see cref="ConfigurationResolveOptions.PathRoot" /> is given: under the EditorConfig-compatible profile an
    /// anchored section applies to a target under that directory, and an unanchored one matches by file name.
    /// </summary>
    [TestMethod]
    public void Load_WhenResolvedWithoutPathRoot_ShouldRootSectionsAtTheFilesDirectory()
    {
        using TempFileScope scope = new("root = true\n[/src/*.cs]\nindent_size = 4\n[*.md]\nindent_size = 2\n");
        var doc = ConfigurationDocument.Load(scope.Path, ConfigurationParseOptions.EditorConfigCompatible);

        ConfigurationView source = doc.Resolve(
            Path.Combine(scope.Directory, "src", "a.cs"),
            ConfigurationResolveOptions.EditorConfigCompatible);
        ConfigurationView notes = doc.Resolve(
            Path.Combine(scope.Directory, "docs", "notes.md"),
            ConfigurationResolveOptions.EditorConfigCompatible);

        Assert.AreEqual("4", source["indent_size"]);
        Assert.AreEqual("2", notes["indent_size"]);
    }

    /// <summary>
    /// Verifies that under the default profile a section anchored without a leading slash applies, in a document loaded
    /// from a path, to a target under the file's directory, and not to one outside it.
    /// </summary>
    [TestMethod]
    public void Load_WhenResolvedWithoutPathRoot_ShouldMatchAnchoredSectionsRelativeToTheFile()
    {
        using TempFileScope scope = new("[src/*.cs]\nformat.indent.size = 4\n");
        var doc = ConfigurationDocument.Load(scope.Path);

        ConfigurationView inside = doc.Resolve(Path.Combine(scope.Directory, "src", "a.cs"));
        ConfigurationView outside = doc.Resolve(Path.Combine(scope.Directory, "lib", "src", "a.cs"));

        Assert.AreEqual("4", inside["format:indent:size"]);
        Assert.IsNull(outside["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that an explicit <see cref="ConfigurationResolveOptions.PathRoot" /> overrides the directory of the file a
    /// document was loaded from.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathRootIsGiven_ShouldOverrideTheFilesDirectory()
    {
        using TempFileScope scope = new("[src/*.cs]\nformat.indent.size = 4\n");
        var doc = ConfigurationDocument.Load(scope.Path);
        string elsewhere = Path.Combine(scope.Directory, "elsewhere");
        var options = new ConfigurationResolveOptions { PathRoot = elsewhere };

        Assert.AreEqual("4", doc.Resolve(Path.Combine(elsewhere, "src", "a.cs"), options)["format:indent:size"]);
        Assert.IsNull(doc.Resolve(Path.Combine(scope.Directory, "src", "a.cs"), options)["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that a document loaded from a path supplies its directory as a root, so resolving it with no target
    /// path under <see cref="ConfigurationMissingPathRootMode.Throw" /> returns the preamble-only view rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Load_WhenResolvedWithoutTargetUnderThrowMode_ShouldReturnThePreambleView()
    {
        using TempFileScope scope = new("indent_style = tab\n[*]\nindent_size = 4\n");
        var doc = ConfigurationDocument.Load(scope.Path);
        var options = new ConfigurationResolveOptions { MissingPathRootMode = ConfigurationMissingPathRootMode.Throw };

        ConfigurationView view = doc.Resolve(null, options);

        CollectionAssert.AreEqual(new[] { "indent_style" }, view.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that a document loaded from a stream or a text reader has no root, so an absolute target is matched as
    /// given and an anchored section does not apply to it, as before.
    /// </summary>
    [TestMethod]
    public void Load_WhenDocumentComesFromStreamOrReader_ShouldRecordNoRoot()
    {
        const string text = "[src/*.cs]\nformat.indent.size = 4\n";
        string absolute = Path.Combine(Path.GetTempPath(), "src", "a.cs");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new StringReader(text);

        var streamed = ConfigurationDocument.Load(stream);
        var read = ConfigurationDocument.Load(reader);

        Assert.IsNull(streamed.Resolve(absolute)["format:indent:size"]);
        Assert.IsNull(read.Resolve(absolute)["format:indent:size"]);
        Assert.AreEqual("4", streamed.Resolve("src/a.cs")["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that each entry of a view resolved from a document loaded by path reports the full path of the file as
    /// its <see cref="ConfigurationSourceLocation.Path" />, for a preamble entry and a section entry alike.
    /// </summary>
    [TestMethod]
    public void Load_WhenResolved_ShouldReportTheFilePathInEachEntrysSourceLocation()
    {
        using TempFileScope scope = new("indent_style = tab\n[*]\nindent_size = 4\n");
        var doc = ConfigurationDocument.Load(scope.Path);

        ConfigurationView view = doc.Resolve(Path.Combine(scope.Directory, "a.cs"));
        ConfigurationResolvedEntry? preamble = view.GetEntry("indent_style");
        ConfigurationResolvedEntry? section = view.GetEntry("indent_size");

        Assert.IsNotNull(preamble);
        Assert.IsNotNull(section);
        Assert.AreEqual(Path.GetFullPath(scope.Path), preamble.SourceLocation.Path);
        Assert.AreEqual(Path.GetFullPath(scope.Path), section.SourceLocation.Path);
    }

    /// <summary>
    /// Verifies that a document loaded through a relative path reports the full path of the file in the source
    /// locations of its resolved entries.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathIsRelative_ShouldReportTheFullPathInEachEntrysSourceLocation()
    {
        using TempFileScope scope = new("[*]\nindent_size = 4\n");
        string relative = Path.GetRelativePath(Environment.CurrentDirectory, scope.Path);
        var doc = ConfigurationDocument.Load(relative);

        ConfigurationResolvedEntry? entry = doc.Resolve("a.cs").GetEntry("indent_size");

        Assert.IsNotNull(entry);
        Assert.AreEqual(Path.GetFullPath(scope.Path), entry.SourceLocation.Path);
    }

    /// <summary>
    /// Verifies that the resolved entries of a document loaded from a stream or a text reader report no path in their
    /// source locations.
    /// </summary>
    [TestMethod]
    public void Load_WhenDocumentComesFromStreamOrReader_ShouldReportNoPathInSourceLocations()
    {
        const string text = "[*]\nindent_size = 4\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new StringReader(text);

        ConfigurationResolvedEntry? streamed = ConfigurationDocument.Load(stream).Resolve("a.cs").GetEntry("indent_size");
        ConfigurationResolvedEntry? read = ConfigurationDocument.Load(reader).Resolve("a.cs").GetEntry("indent_size");

        Assert.IsNotNull(streamed);
        Assert.IsNotNull(read);
        Assert.IsNull(streamed.SourceLocation.Path);
        Assert.IsNull(read.SourceLocation.Path);
    }
}
