// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationIniDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Configuration;
using Bodu.Text.Ini;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies that the in-memory <see cref="IniDocument" /> overload of <c>AddBoduConfigurationDocument</c> resolves a
/// pre-parsed document and flattens it into the configuration view via <c>AddInMemoryCollection</c>.
/// </summary>
[TestClass]
public class TextConfigurationIniDocumentTests
{
    private const string Sample = """
logging.level.default = Information

[src/**/*.cs]
logging.level.default = Warning
""";

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload flattens a parsed document into the configuration
    /// view exactly as the file overload would.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithIniDocument_ShouldFlattenIntoConfiguration()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        var document = ConfigurationDocument.Load(stream, ConfigurationParseOptions.Bodu);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfigurationDocument(document)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload honours a supplied target path during resolution.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithIniDocument_WhenTargetPathProvided_ShouldApplyMatchingSection()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        var document = ConfigurationDocument.Load(stream, ConfigurationParseOptions.Bodu);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfigurationDocument(document, targetPath: "src/Foo.cs")
            .Build();

        Assert.AreEqual("Warning", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload rejects a <see langword="null" /> document.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithIniDocument_WhenDocumentIsNull_ShouldThrowExactly()
    {
        IniDocument document = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddBoduConfigurationDocument(document);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload rejects a <see langword="null" /> builder.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithIniDocument_WhenBuilderIsNull_ShouldThrowExactly()
    {
        IConfigurationBuilder builder = null!;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        var document = ConfigurationDocument.Load(stream, ConfigurationParseOptions.Bodu);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddBoduConfigurationDocument(document);
        });
    }
}
