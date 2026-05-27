// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.AddPlugin.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

public partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddPlugin</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddPlugin_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.AddPlugin(null!, new StubPlugin());
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>AddPlugin</c> throws when the plugin is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddPlugin_WhenPluginIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddPlugin(null!);
        });

        Assert.AreEqual("plugin", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an <see cref="INotableDateRulePlugin" /> registered via <c>AddPlugin</c> contributes its rules to
    /// the resolved <see cref="INotableDateService" />.
    /// </summary>
    [TestMethod]
    public void AddPlugin_WhenRulePluginRegistered_ShouldContributePluginRulesToService()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.AddPlugin(new StubRulePlugin(Fixed("Plugin Day", 4, 4)));

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.Contains(n => n.Name == "Plugin Day", service.GetNotableDates(2026));
    }

    /// <summary>
    /// Verifies that <c>AddPlugin</c> returns the same builder instance so calls can be chained.
    /// </summary>
    [TestMethod]
    public void AddPlugin_WhenCalled_ShouldReturnSameBuilderInstance()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        INotableDateServiceBuilder returned = builder.AddPlugin(new StubPlugin());

        Assert.AreSame(builder, returned);
    }

    /// <summary>
    /// A minimal <see cref="INotableDatePlugin" /> stub that contributes neither rules nor algorithms — used to verify
    /// the registration path without affecting service output.
    /// </summary>
    private sealed class StubPlugin : INotableDatePlugin
    {
        /// <inheritdoc />
        public string Name => "Stub";

        /// <inheritdoc />
        public Version Version { get; } = new(1, 0, 0);
    }

    /// <summary>
    /// A stub plugin that contributes a single in-memory rule provider so that registration can be verified through
    /// the resolved service surface.
    /// </summary>
    private sealed class StubRulePlugin : INotableDatePlugin, INotableDateRulePlugin
    {
        /// <summary>
        /// The rule providers contributed by this plugin.
        /// </summary>
        private readonly INotableDateRuleProvider[] _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubRulePlugin" /> class with the supplied rules.
        /// </summary>
        /// <param name="rules">The rules wrapped in a single in-memory provider.</param>
        public StubRulePlugin(params NotableDateRule[] rules)
        {
            _providers = [new InMemoryRuleProvider(rules)];
        }

        /// <inheritdoc />
        public string Name => "Stub Rule Plugin";

        /// <inheritdoc />
        public Version Version { get; } = new(1, 0, 0);

        /// <inheritdoc />
        public IEnumerable<INotableDateRuleProvider> GetRuleProviders() => _providers;
    }
}
