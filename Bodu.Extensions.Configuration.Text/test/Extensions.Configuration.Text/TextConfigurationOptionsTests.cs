// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationOptionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies the <see cref="ConfigurationOptionsExtensions" /> helpers for binding configuration sections
/// to <see cref="IOptions{TOptions}" /> through the dependency-injection container.
/// </summary>
[TestClass]
public class TextConfigurationOptionsTests
{
    private const string Sample = """
service.name = Bodu
service.port = 8080
""";

    /// <summary>
    /// Verifies that
    /// <see cref="ConfigurationOptionsExtensions.AddConfigurationOptions{TOptions}(IServiceCollection, IConfiguration, string)" />
    /// binds the named section to the options instance resolved from the container.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WhenBound_ShouldDeliverViaIOptions()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        ServiceCollection services = new();
        services.AddOptions();
        services.AddConfigurationOptions<ServiceOptions>(configuration, "service");

        using ServiceProvider provider = services.BuildServiceProvider();
        ServiceOptions options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;

        Assert.AreEqual("Bodu", options.Name);
        Assert.AreEqual(8080, options.Port);
    }

    /// <summary>
    /// Verifies that the section-overload of <c>AddConfigurationOptions</c> resolves an equivalent options
    /// instance.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WithSection_ShouldDeliverViaIOptions()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        ServiceCollection services = new();
        services.AddOptions();
        services.AddConfigurationOptions<ServiceOptions>(configuration.GetSection("service"));

        using ServiceProvider provider = services.BuildServiceProvider();
        ServiceOptions options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;

        Assert.AreEqual("Bodu", options.Name);
        Assert.AreEqual(8080, options.Port);
    }

    /// <summary>
    /// Verifies that the name-based overload rejects a <see langword="null" /> service collection.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WhenServicesIsNull_ShouldThrowArgumentNullException()
    {
        IServiceCollection services = null!;
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddConfigurationOptions<ServiceOptions>(configuration, "service");
        });
    }

    /// <summary>
    /// Verifies that the name-based overload rejects a <see langword="null" /> configuration.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
    {
        ServiceCollection services = new();
        IConfiguration configuration = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddConfigurationOptions<ServiceOptions>(configuration, "service");
        });
    }

    /// <summary>
    /// Verifies that the name-based overload rejects a whitespace section name.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WhenSectionNameIsWhitespace_ShouldThrowArgumentException()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = services.AddConfigurationOptions<ServiceOptions>(configuration, "   ");
        });
    }

    /// <summary>
    /// Verifies that the section overload rejects a <see langword="null" /> service collection.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WithSection_WhenServicesIsNull_ShouldThrowArgumentNullException()
    {
        IServiceCollection services = null!;
        IConfigurationSection section = new ConfigurationBuilder().Build().GetSection("anything");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddConfigurationOptions<ServiceOptions>(section);
        });
    }

    /// <summary>
    /// Verifies that the section overload rejects a <see langword="null" /> section.
    /// </summary>
    [TestMethod]
    public void AddConfigurationOptions_WithSection_WhenSectionIsNull_ShouldThrowArgumentNullException()
    {
        ServiceCollection services = new();
        IConfigurationSection section = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = services.AddConfigurationOptions<ServiceOptions>(section);
        });
    }

    /// <summary>
    /// POCO used to validate IOptions binding semantics in this fixture.
    /// </summary>
    private sealed class ServiceOptions
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the service port.
        /// </summary>
        public int Port { get; set; }
    }
}
