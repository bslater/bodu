// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.Save.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Save(IniDocument, string, ConfigurationWriteOptions?)" />
    /// writes a file that parses back to an equivalent document.
    /// </summary>
    [TestMethod]
    public void Save_WhenPathProvided_ShouldRoundTripThroughDisk()
    {
        var original = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);
        string outPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".boduconfig");
        try
        {
            ConfigurationDocument.Save(original, outPath);

            var reloaded = ConfigurationDocument.Load(outPath);

            Assert.HasCount(original.Sections.Count, reloaded.Sections);
            Assert.HasCount(original.Sections[0].Entries.Count, reloaded.Sections[0].Entries);
        }
        finally
        {
            File.Delete(outPath);
        }
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Save(IniDocument, string, ConfigurationWriteOptions?)" />
    /// rejects a <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void Save_WhenPathIsNull_ShouldThrowExactly()
    {
        IniDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ConfigurationDocument.Save(doc, (string)null!);
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
            ConfigurationDocument.Save(doc, stream);
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
            ConfigurationDocument.Save(doc, (Stream)null!);
        });
    }
}
