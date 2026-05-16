// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationStreamTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies the stream-based <c>AddBoduConfiguration</c> overloads that mirror
/// <c>AddJsonStream</c> from <c>Microsoft.Extensions.Configuration.Json</c>.
/// </summary>
[TestClass]
public class BoduTextConfigurationStreamTests
{
    private const string Sample = """
logging.level.default = Information

[src/**/*.cs]
logging.level.default = Warning
""";

    /// <summary>
    /// Verifies that <see cref="BoduTextConfigurationExtensions.AddBoduConfiguration(IConfigurationBuilder, Stream, string?, BoduConfigurationParseOptions?, BoduConfigurationResolveOptions?)" />
    /// reads from a stream and exposes the resulting keys in colon-delimited form.
    /// </summary>
    [TestMethod]
    public void AddBoduConfigurationStream_ShouldExposeColonDelimitedKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfiguration(stream)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the stream overload honours a supplied target path and applies the matching glob-anchored
    /// section override.
    /// </summary>
    [TestMethod]
    public void AddBoduConfigurationStream_WhenTargetPathProvided_ShouldApplyMatchingSection()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfiguration(stream, targetPath: "src/Foo.cs")
            .Build();

        Assert.AreEqual("Warning", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the action-callback stream overload yields the same view as the direct stream overload.
    /// </summary>
    [TestMethod]
    public void AddBoduConfigurationStream_WhenConfiguredViaCallback_ShouldExposeColonDelimitedKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfiguration(source =>
            {
                source.Stream = stream;
                source.TargetPath = "src/Foo.cs";
            })
            .Build();

        Assert.AreEqual("Warning", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the stream overload rejects a <see langword="null" /> stream.
    /// </summary>
    [TestMethod]
    public void AddBoduConfigurationStream_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        Stream stream = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddBoduConfiguration(stream);
        });
    }

    /// <summary>
    /// Verifies that the stream-callback overload rejects a <see langword="null" /> configuration callback.
    /// </summary>
    [TestMethod]
    public void AddBoduConfigurationStream_WhenCallbackIsNull_ShouldThrowArgumentNullException()
    {
        Action<BoduTextStreamConfigurationSource> configure = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddBoduConfiguration(configure);
        });
    }

    /// <summary>
    /// Verifies that supplied parse and resolve options propagate through the stream source to the
    /// configuration values.
    /// </summary>
    [TestMethod]
    public void AddBoduConfigurationStream_WhenOptionsProvided_ShouldUseTheOptions()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfiguration(
                stream,
                targetPath: null,
                parseOptions: BoduConfigurationParseOptions.Bodu,
                resolveOptions: BoduConfigurationResolveOptions.Bodu)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }
}
