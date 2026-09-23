// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderExtensionsTests.Guards.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderExtensionsTests
{
    /// <summary>
    /// Verifies that registering against a <see langword="null" /> builder throws
    /// <see cref="ArgumentNullException" /> naming the builder parameter.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((IFinancialServiceBuilder)null!).AddWebRateProvider<TestRateProvider, TestRateProviderOptions>(
                TestHttpClientName,
                null,
                TestSectionName,
                TestValidationMessage,
                DefaultConfigure,
                null,
                static (client, options, loggerFactory, timeProvider) =>
                    new TestRateProvider(client, options, loggerFactory, timeProvider));
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> section name throws <see cref="ArgumentNullException" />, since the
    /// registration binds options from that section.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenSectionNameIsNull_ShouldThrowArgumentNullException()
    {
        IFinancialServiceBuilder builder = new ServiceCollection().AddFinancialService();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddWebRateProvider<TestRateProvider, TestRateProviderOptions>(
                TestHttpClientName,
                null,
                null!,
                TestValidationMessage,
                DefaultConfigure,
                null,
                static (client, options, loggerFactory, timeProvider) =>
                    new TestRateProvider(client, options, loggerFactory, timeProvider));
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty or whitespace section name throws <see cref="ArgumentException" /> rather than binding
    /// options from the configuration root.
    /// </summary>
    /// <param name="sectionName">The blank section name under test.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void AddWebRateProvider_WhenSectionNameIsBlank_ShouldThrowArgumentException(string sectionName)
    {
        IFinancialServiceBuilder builder = new ServiceCollection().AddFinancialService();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.AddWebRateProvider<TestRateProvider, TestRateProviderOptions>(
                TestHttpClientName,
                null,
                sectionName,
                TestValidationMessage,
                DefaultConfigure,
                null,
                static (client, options, loggerFactory, timeProvider) =>
                    new TestRateProvider(client, options, loggerFactory, timeProvider));
        });

        Assert.AreEqual("sectionName", ex.ParamName);
    }
}
