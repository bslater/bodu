// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.Load.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Load(string)" /> reads and parses a file from disk.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathExists_ShouldProduceDocument()
    {
        using TempFileScope scope = new(BoduConfigurationFixtures.Representative);

        IniDocument doc = BoduConfigurationDocument.Load(scope.Path);

        Assert.AreEqual(2, doc.Sections.Count);
        Assert.AreEqual("true", doc.GlobalSection["root"]);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Load(string)" /> rejects a <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoduConfigurationDocument.Load((string)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Load(string)" /> rejects a whitespace path with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathIsWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BoduConfigurationDocument.Load("   ");
        });
    }

    /// <summary>
    /// Verifies that loading from a stream produces the same document as loading from text.
    /// </summary>
    [TestMethod]
    public void Load_WhenStreamProvided_ShouldProduceSameDocumentAsParse()
    {
        IniDocument fromText = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(BoduConfigurationFixtures.Representative));
        IniDocument fromStream = BoduConfigurationDocument.Load(stream);

        Assert.AreEqual(fromText.Sections.Count, fromStream.Sections.Count);
        Assert.AreEqual(fromText.GlobalSection.Entries.Count, fromStream.GlobalSection.Entries.Count);
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
            _ = BoduConfigurationDocument.Load(stream);
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
            _ = BoduConfigurationDocument.Load((Stream)null!);
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
            _ = BoduConfigurationDocument.Load((TextReader)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.LineNumber" /> reflects the source line on a loaded document.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathProvided_ShouldAttachLineNumberToEntries()
    {
        using TempFileScope scope = new(BoduConfigurationFixtures.Minimal);

        IniDocument doc = BoduConfigurationDocument.Load(scope.Path);

        Assert.AreEqual(2, doc.Sections[0].Entries[0].LineNumber);
    }
}
