// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConfigurationTests.Provider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class BencodeConfigurationTests
{
    /// <summary>
    /// Verifies that the provider rejects mutation through <see cref="IConfigurationProvider.Set" /> with
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Provider_WhenSet_ShouldRejectMutation()
    {
        var provider = new BencodeConfigurationProvider(new BencodeConfigurationSource());

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            provider.Set("key", "value");
        });
    }

    /// <summary>
    /// Verifies that constructing a provider from a <see langword="null" /> source throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Provider_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodeConfigurationProvider(null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a source with neither a stream nor a path loads an empty configuration map.
    /// </summary>
    [TestMethod]
    public void Provider_WhenNoStreamOrPath_ShouldLoadEmpty()
    {
        var provider = new BencodeConfigurationProvider(new BencodeConfigurationSource());
        provider.Load();

        Assert.IsFalse(provider.TryGet("anything", out _));
    }

    /// <summary>
    /// Verifies that the provider implements <see cref="IConfigurationProvider" /> and exposes a non-null,
    /// never-firing reload token.
    /// </summary>
    [TestMethod]
    public void Provider_ShouldImplementConfigurationProvider()
    {
        var provider = new BencodeConfigurationProvider(new BencodeConfigurationSource());

        Assert.IsInstanceOfType<IConfigurationProvider>(provider);
        Assert.IsNotNull(provider.GetReloadToken());
        Assert.IsFalse(provider.GetReloadToken().HasChanged);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeConfigurationSource.Build(IConfigurationBuilder)" /> produces a
    /// <see cref="BencodeConfigurationProvider" />.
    /// </summary>
    [TestMethod]
    public void Source_WhenBuilt_ShouldProduceBencodeProvider()
    {
        var source = new BencodeConfigurationSource();

        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());

        Assert.IsInstanceOfType<BencodeConfigurationProvider>(provider);
    }
}
