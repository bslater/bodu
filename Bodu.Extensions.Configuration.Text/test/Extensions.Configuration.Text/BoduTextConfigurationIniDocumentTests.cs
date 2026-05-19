// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationIniDocumentTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;
using Bodu.Text.Ini;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies that the in-memory <see cref="IniDocument" /> overload of <c>AddBoduConfiguration</c> resolves a
/// pre-parsed document and flattens it into the configuration view via <c>AddInMemoryCollection</c>.
/// </summary>
[TestClass]
public class BoduTextConfigurationIniDocumentTests
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
    public void AddBoduConfiguration_WithIniDocument_ShouldFlattenIntoConfiguration()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        IniDocument document = BoduConfigurationDocument.Load(stream, BoduConfigurationParseOptions.Bodu);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfiguration(document)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload honours a supplied target path during resolution.
    /// </summary>
    [TestMethod]
    public void AddBoduConfiguration_WithIniDocument_WhenTargetPathProvided_ShouldApplyMatchingSection()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        IniDocument document = BoduConfigurationDocument.Load(stream, BoduConfigurationParseOptions.Bodu);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfiguration(document, targetPath: "src/Foo.cs")
            .Build();

        Assert.AreEqual("Warning", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload rejects a <see langword="null" /> document.
    /// </summary>
    [TestMethod]
    public void AddBoduConfiguration_WithIniDocument_WhenDocumentIsNull_ShouldThrowArgumentNullException()
    {
        IniDocument document = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddBoduConfiguration(document);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="IniDocument" /> overload rejects a <see langword="null" /> builder.
    /// </summary>
    [TestMethod]
    public void AddBoduConfiguration_WithIniDocument_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        IConfigurationBuilder builder = null!;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        IniDocument document = BoduConfigurationDocument.Load(stream, BoduConfigurationParseOptions.Bodu);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddBoduConfiguration(document);
        });
    }
}
