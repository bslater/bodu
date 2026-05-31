// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationDuplicateKeyTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Configuration;
using Bodu.Text.Ini;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Pins how the bridge handles duplicate keys and duplicate sections under the Bodu default profile and
/// under the strict profile. The Bodu provider defaults to last-wins (consistent with EditorConfig-style
/// layering) but callers can opt into fail-fast behaviour through <see cref="ConfigurationParseOptions" />.
/// </summary>
[TestClass]
public class TextConfigurationDuplicateKeyTests
{
    /// <summary>
    /// Verifies that duplicate keys inside a single section resolve to the last-written value under the
    /// default (Bodu) profile, matching the document parser's <see cref="IniDuplicateKeyBehavior.LastWins" />
    /// default.
    /// </summary>
    [TestMethod]
    public void Build_WhenDuplicateKeysInPreambleAndDefaultProfile_ShouldUseLastWritten()
    {
        const string sample = """
service.name = First
service.name = Second
""";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        Assert.AreEqual("Second", configuration["service:name"]);
    }

    /// <summary>
    /// Verifies that the same fixture under the Strict parse profile throws because duplicate keys are
    /// disallowed.
    /// </summary>
    [TestMethod]
    public void Build_WhenDuplicateKeysAndStrictProfile_ShouldThrow()
    {
        const string sample = """
service.name = First
service.name = Second
""";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(sample));

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: null, parseOptions: ConfigurationParseOptions.Strict);

        Assert.ThrowsExactly<ConfigurationParseException>(() => _ = builder.Build());
    }

    /// <summary>
    /// Verifies that when the same key is set in multiple matching sections, the last matching section
    /// wins after resolution — confirming the bridge preserves the document resolver's section ordering.
    /// </summary>
    [TestMethod]
    public void Build_WhenSameKeyInMultipleMatchingSections_ShouldUseLastMatchingSection()
    {
        const string sample = """
[*.cs]
format.indent.size = 4

[src/**/*.cs]
format.indent.size = 2
""";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: "src/Foo.cs")
            .Build();

        Assert.AreEqual("2", configuration["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that when the same key is set across multiple <em>providers</em>, Microsoft's
    /// last-added-provider-wins rule applies — the second <c>AddConfiguration</c> call overrides the first.
    /// </summary>
    [TestMethod]
    public void Build_WhenSameKeyInMultipleProviders_ShouldUseLastAddedProvider()
    {
        const string first = "service.name = First\n";
        const string second = "service.name = Second\n";

        using MemoryStream a = new(Encoding.UTF8.GetBytes(first));
        using MemoryStream b = new(Encoding.UTF8.GetBytes(second));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(a)
            .AddConfiguration(b)
            .Build();

        Assert.AreEqual("Second", configuration["service:name"]);
    }
}
