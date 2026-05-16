// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.Save.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(string, BoduConfigurationWriteOptions?)" />
    /// writes a file that parses back to an equivalent document.
    /// </summary>
    [TestMethod]
    public void Save_WhenPathProvided_ShouldRoundTripThroughDisk()
    {
        BoduConfigurationDocument original = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        string outPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".boduconfig");
        try
        {
            original.Save(outPath);

            BoduConfigurationDocument reloaded = BoduConfigurationDocument.Load(outPath);

            Assert.AreEqual(original.Sections.Count, reloaded.Sections.Count);
            Assert.AreEqual(original.Sections[0].Properties.Count, reloaded.Sections[0].Properties.Count);
        }
        finally
        {
            File.Delete(outPath);
        }
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(string, BoduConfigurationWriteOptions?)" />
    /// rejects a <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void Save_WhenPathIsNull_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            doc.Save((string)null!);
        });
    }

    /// <summary>
    /// Verifies that saving to a stream that does not support writing throws an
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Save_WhenStreamCannotWrite_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();
        using MemoryStream stream = new(new byte[16], writable: false);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            doc.Save(stream);
        });
    }

    /// <summary>
    /// Verifies that saving to a <see langword="null" /> stream throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Save_WhenStreamIsNull_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            doc.Save((Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.ToString" /> produces text that parses to an
    /// equivalent document.
    /// </summary>
    [TestMethod]
    public void ToString_WhenInvoked_ShouldProduceParseableText()
    {
        BoduConfigurationDocument original = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        string text = original.ToString();

        BoduConfigurationDocument reparsed = BoduConfigurationDocument.Parse(text);

        Assert.AreEqual(original.Sections.Count, reparsed.Sections.Count);
    }
}
