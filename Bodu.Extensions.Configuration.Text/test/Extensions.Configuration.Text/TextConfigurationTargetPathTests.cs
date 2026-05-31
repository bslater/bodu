// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationTargetPathTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Pins the <see cref="TextConfigurationSource.TargetPath" /> behaviour for the bridge providers. When
/// <c>TargetPath</c> is <see langword="null" /> only preamble (global section) keys flow into the
/// configuration view — section-anchored values are not surfaced, because there is no path against which the
/// section globs can be evaluated.
/// </summary>
[TestClass]
public class TextConfigurationTargetPathTests
{
    private const string Sample = """
service.name = Bodu
service.port = 8080

[*.cs]
format.indent.size = 4

[src/**/*.cs]
format.indent.size = 2
""";

    /// <summary>
    /// Verifies that when <see cref="TextConfigurationSource.TargetPath" /> is <see langword="null" />,
    /// only preamble keys are visible in the configuration view; matching-section keys remain absent.
    /// </summary>
    [TestMethod]
    public void Build_WhenTargetPathIsNull_ShouldExposeOnlyPreambleKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: null)
            .Build();

        Assert.AreEqual("Bodu", configuration["service:name"]);
        Assert.AreEqual("8080", configuration["service:port"]);
        Assert.IsNull(configuration["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that a target path matching a single section overlays that section's values on top of the
    /// preamble.
    /// </summary>
    [TestMethod]
    public void Build_WhenTargetPathMatchesSingleSection_ShouldOverlayMatchingSection()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: "Foo.cs")
            .Build();

        Assert.AreEqual("4", configuration["format:indent:size"]);
        Assert.AreEqual("Bodu", configuration["service:name"]);
    }

    /// <summary>
    /// Verifies that a target path matching multiple sections applies them in source order, with the later
    /// section's value winning.
    /// </summary>
    [TestMethod]
    public void Build_WhenTargetPathMatchesMultipleSections_ShouldApplyLastMatchingSection()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: "src/Foo.cs")
            .Build();

        Assert.AreEqual("2", configuration["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that a target path with no matching sections still exposes preamble values, mirroring the
    /// <c>Resolve_WhenNoSectionMatches_ShouldStillApplyPreamble</c> behaviour of the document resolver.
    /// </summary>
    [TestMethod]
    public void Build_WhenTargetPathHasNoMatch_ShouldStillExposePreamble()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: "README.md")
            .Build();

        Assert.AreEqual("Bodu", configuration["service:name"]);
        Assert.IsNull(configuration["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that when both <see cref="TextConfigurationSource.TargetPath" /> is <see langword="null" />
    /// and the resolve options disable preamble layering, the configuration view is empty.
    /// </summary>
    [TestMethod]
    public void Build_WhenTargetPathIsNullAndPreambleDisabled_ShouldExposeNothing()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        ConfigurationResolveOptions resolve = new() { ApplyPreambleProperties = false };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: null, parseOptions: null, resolveOptions: resolve)
            .Build();

        Assert.IsNull(configuration["service:name"]);
        Assert.IsNull(configuration["format:indent:size"]);
    }
}
