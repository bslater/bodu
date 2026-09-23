// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderExtensionsTests.Options.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderExtensionsTests
{
    /// <summary>
    /// Verifies that options are bound from the named configuration section rather than the configuration root.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenConfigurationSupplied_ShouldBindOptionsFromNamedSection()
    {
        IConfigurationRoot configuration = CreateConfiguration(
            ("BaseAddress", "https://bound.example.test/"),
            ("HttpTimeout", "00:00:11"));

        using ServiceProvider provider = CreateServices(configuration, configure: static _ => { })
            .BuildServiceProvider();

        TestRateProviderOptions options = provider.GetRequiredService<IOptions<TestRateProviderOptions>>().Value;

        Assert.AreEqual(new Uri("https://bound.example.test/"), options.BaseAddress);
        Assert.AreEqual(TimeSpan.FromSeconds(11), options.HttpTimeout);
    }

    /// <summary>
    /// Verifies that the <c>configure</c> delegate is applied after configuration binding, so code-based
    /// configuration wins over a bound value.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenBothConfigurationAndConfigureSupplied_ShouldApplyConfigureLast()
    {
        IConfigurationRoot configuration = CreateConfiguration(("BaseAddress", "https://bound.example.test/"));
        var expected = new Uri("https://override.example.test/");

        using ServiceProvider provider = CreateServices(configuration, configure: o => o.BaseAddress = expected)
            .BuildServiceProvider();

        TestRateProviderOptions options = provider.GetRequiredService<IOptions<TestRateProviderOptions>>().Value;

        Assert.AreEqual(expected, options.BaseAddress);
    }

    /// <summary>
    /// Verifies that omitting configuration leaves the defaults in place for the <c>configure</c> delegate to
    /// populate, rather than failing to register.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenConfigurationIsNull_ShouldStillApplyConfigureDelegate()
    {
        var expected = new Uri("https://code-only.example.test/");

        using ServiceProvider provider = CreateServices(configuration: null, configure: o => o.BaseAddress = expected)
            .BuildServiceProvider();

        TestRateProviderOptions options = provider.GetRequiredService<IOptions<TestRateProviderOptions>>().Value;

        Assert.AreEqual(expected, options.BaseAddress);
    }

    /// <summary>
    /// Verifies that options failing <see cref="WebRateProviderOptions.TryValidate" /> throw
    /// <see cref="OptionsValidationException" /> carrying the registered validation message, so misconfiguration is
    /// reported with the provider's own wording.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenOptionsAreInvalid_ShouldThrowOptionsValidationExceptionWithRegisteredMessage()
    {
        // BaseAddress left null — the first invariant TryValidate checks.
        using ServiceProvider provider = CreateServices(configure: static _ => { }).BuildServiceProvider();

        var ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<TestRateProviderOptions>>().Value;
        });

        Assert.IsTrue(
            ex.Failures.Any(f => f.Contains(TestValidationMessage, StringComparison.Ordinal)),
            $"Expected the registered validation message. Actual failures: {string.Join(" | ", ex.Failures)}");
    }

    /// <summary>
    /// Verifies that a non-positive HTTP timeout is rejected by validation, confirming the shared invariants are
    /// wired in rather than only the presence of a base address.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenHttpTimeoutIsNotPositive_ShouldThrowOptionsValidationException()
    {
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.HttpTimeout = TimeSpan.Zero;
        }).BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<TestRateProviderOptions>>().Value;
        });
    }
}
