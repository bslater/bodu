// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationStreamTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies the stream-based <c>AddConfiguration</c> overloads that mirror
/// <c>AddJsonStream</c> from <c>Microsoft.Extensions.Configuration.Json</c>.
/// </summary>
[TestClass]
public class TextConfigurationStreamTests
{
    private const string Sample = """
logging.level.default = Information

[src/**/*.cs]
logging.level.default = Warning
""";

    /// <summary>
    /// Verifies that <see cref="TextConfigurationExtensions.AddConfiguration(IConfigurationBuilder, Stream, string?, ConfigurationParseOptions?, ConfigurationResolveOptions?)" />
    /// reads from a stream and exposes the resulting keys in colon-delimited form.
    /// </summary>
    [TestMethod]
    public void AddConfigurationStream_ShouldExposeColonDelimitedKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the stream overload honours a supplied target path and applies the matching glob-anchored
    /// section override.
    /// </summary>
    [TestMethod]
    public void AddConfigurationStream_WhenTargetPathProvided_ShouldApplyMatchingSection()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: "src/Foo.cs")
            .Build();

        Assert.AreEqual("Warning", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the action-callback stream overload yields the same view as the direct stream overload.
    /// </summary>
    [TestMethod]
    public void AddConfigurationStream_WhenConfiguredViaCallback_ShouldExposeColonDelimitedKeys()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(source =>
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
    public void AddConfigurationStream_WhenStreamIsNull_ShouldThrowExactly()
    {
        Stream stream = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddConfiguration(stream);
        });
    }

    /// <summary>
    /// Verifies that the stream-callback overload rejects a <see langword="null" /> configuration callback.
    /// </summary>
    [TestMethod]
    public void AddConfigurationStream_WhenCallbackIsNull_ShouldThrowExactly()
    {
        Action<TextStreamConfigurationSource> configure = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationBuilder().AddConfiguration(configure);
        });
    }

    /// <summary>
    /// Verifies that supplied parse and resolve options propagate through the stream source to the
    /// configuration values.
    /// </summary>
    [TestMethod]
    public void AddConfigurationStream_WhenOptionsProvided_ShouldUseTheOptions()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(
                stream,
                targetPath: null,
                parseOptions: ConfigurationParseOptions.Bodu,
                resolveOptions: ConfigurationResolveOptions.Bodu)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }
}
