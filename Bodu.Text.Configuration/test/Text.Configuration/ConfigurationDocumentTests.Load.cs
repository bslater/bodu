// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.Load.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Ini;

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

        IniDocument doc = ConfigurationDocument.Load(scope.Path);

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
        IniDocument fromText = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(ConfigurationFixtures.Representative));
        IniDocument fromStream = ConfigurationDocument.Load(stream);

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

        IniDocument doc = ConfigurationDocument.Load(scope.Path);

        Assert.AreEqual(2, doc.Sections[0].Entries[0].LineNumber);
    }
}
