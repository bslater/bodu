// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBindingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies that the bridge produces a configuration view compatible with standard
/// <c>Microsoft.Extensions.Configuration</c> consumer patterns (POCO binding, section enumeration,
/// <c>GetSection</c>).
/// </summary>
[TestClass]
public class TextConfigurationBindingTests
{
    private const string PocoSample = """
logging.level.default = Information
logging.level.console = Warning
logging.provider = Console
""";

    /// <summary>
    /// Verifies that <see cref="ConfigurationBinder.Get{T}(IConfiguration)" /> populates a POCO whose property
    /// names match the colon-delimited keys.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSectionMapsToPoco_ShouldPopulateFields()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(PocoSample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        LoggingOptions options = configuration.GetSection("logging").Get<LoggingOptions>() ?? new();

        Assert.AreEqual("Console", options.Provider);
        Assert.IsNotNull(options.Level);
        Assert.AreEqual("Information", options.Level!["default"]);
        Assert.AreEqual("Warning", options.Level!["console"]);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> exposes the nested keys via
    /// <see cref="IConfiguration.GetChildren" /> using the leaf segments as child keys.
    /// </summary>
    [TestMethod]
    public void GetChildren_WhenSectionHasNestedKeys_ShouldEnumerateLeafKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(PocoSample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        IConfigurationSection level = configuration.GetSection("logging:level");
        var childKeys = level.GetChildren().Select(c => c.Key).OrderBy(k => k).ToList();

        CollectionAssert.AreEquivalent(new[] { "console", "default" }, childKeys);
        Assert.AreEqual("Information", level["default"]);
        Assert.AreEqual("Warning", level["console"]);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> returns an existing section whose
    /// <see cref="IConfigurationSection.Path" /> matches the requested path.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenSectionExists_ShouldExposePathAndKey()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(PocoSample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        IConfigurationSection section = configuration.GetSection("logging");

        Assert.AreEqual("logging", section.Path);
        Assert.AreEqual("logging", section.Key);
        Assert.AreEqual("Console", section["provider"]);
    }

    /// <summary>
    /// POCO used to validate binding semantics for the logging section.
    /// </summary>
    private sealed class LoggingOptions
    {
        /// <summary>
        /// Gets or sets the provider identifier.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Gets or sets the per-category log level map keyed by category name.
        /// </summary>
        public Dictionary<string, string>? Level { get; set; }
    }
}
