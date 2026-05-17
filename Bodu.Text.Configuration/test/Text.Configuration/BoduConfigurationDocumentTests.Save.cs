// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.Save.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(IniDocument, string, BoduConfigurationWriteOptions?)" />
    /// writes a file that parses back to an equivalent document.
    /// </summary>
    [TestMethod]
    public void Save_WhenPathProvided_ShouldRoundTripThroughDisk()
    {
        IniDocument original = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        var outPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".boduconfig");
        try
        {
            BoduConfigurationDocument.Save(original, outPath);

            IniDocument reloaded = BoduConfigurationDocument.Load(outPath);

            Assert.HasCount(original.Sections.Count, reloaded.Sections);
            Assert.HasCount(original.Sections[0].Entries.Count, reloaded.Sections[0].Entries);
        }
        finally
        {
            File.Delete(outPath);
        }
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(IniDocument, string, BoduConfigurationWriteOptions?)" />
    /// rejects a <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void Save_WhenPathIsNull_ShouldThrowExactly()
    {
        IniDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            BoduConfigurationDocument.Save(doc, (string)null!);
        });
    }

    /// <summary>
    /// Verifies that saving to a stream that does not support writing throws an
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Save_WhenStreamCannotWrite_ShouldThrowExactly()
    {
        IniDocument doc = new();
        using MemoryStream stream = new(new byte[16], writable: false);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            BoduConfigurationDocument.Save(doc, stream);
        });
    }

    /// <summary>
    /// Verifies that saving to a <see langword="null" /> stream throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Save_WhenStreamIsNull_ShouldThrowExactly()
    {
        IniDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            BoduConfigurationDocument.Save(doc, (Stream)null!);
        });
    }
}
