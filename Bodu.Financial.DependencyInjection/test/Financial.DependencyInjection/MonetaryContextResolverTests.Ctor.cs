// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolverTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextResolverTests
{
    /// <summary>
    /// Verifies that resolving the resolver over settings that fail validation throws
    /// <see cref="OptionsValidationException" /> with one failure for each invalid context, naming it.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsInvalid_ShouldThrowOptionsValidationExceptionNamingEachContext()
    {
        (_, ServiceProvider provider) = Build(new()
        {
            [DefaultKey + "ScalePolicy"] = "Custom",
            [NamedKey + "Invoicing:ScalePolicy"] = "Custom",
            [NamedKey + "Invoicing:CustomScale"] = "40",
            [NamedKey + "Retail:Rounding"] = "AwayFromZero",
        });

        var ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IMonetaryContextResolver>();
        });

        Assert.AreEqual(typeof(FinancialOptions), ex.OptionsType);
        Assert.HasCount(2, ex.Failures);
        Assert.IsTrue(ex.Failures.Any(failure => failure.Contains("default monetary context", StringComparison.Ordinal)), ex.Message);
        Assert.IsTrue(ex.Failures.Any(failure => failure.Contains("'Invoicing'", StringComparison.Ordinal)), ex.Message);
    }

    /// <summary>
    /// Verifies that a named context whose name is white space is reported as a validation failure.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenContextNameBlank_ShouldReportTheBlankName()
    {
        IServiceCollection services = new ServiceCollection().AddFinancialService().Services;
        services.Configure<FinancialOptions>(options => options.MonetaryContexts[" "] = new MonetaryContextOptions());
        ServiceProvider provider = services.BuildServiceProvider();

        var ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IMonetaryContextResolver>();
        });

        Assert.AreEqual(DependencyInjectionResourceStrings.Op_Invalid_MonetaryContextOptionsNameBlank, ex.Failures.Single());
    }

    /// <summary>
    /// Verifies that a named context whose settings are <see langword="null" /> is reported as a validation failure
    /// naming it.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenContextSettingsNull_ShouldReportTheMissingSettings()
    {
        IServiceCollection services = new ServiceCollection().AddFinancialService().Services;
        services.Configure<FinancialOptions>(options => options.MonetaryContexts["Retail"] = null!);
        ServiceProvider provider = services.BuildServiceProvider();

        var ex = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IMonetaryContextResolver>();
        });

        StringAssert.Contains(ex.Failures.Single(), "'Retail'");
    }

    /// <summary>
    /// Verifies that the resolver rejects a <see langword="null" /> options monitor with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMonitorNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MonetaryContextResolver(null!, new ServiceCollection().BuildServiceProvider(), logger: null);
        });

        Assert.AreEqual("monitor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the resolver rejects a <see langword="null" /> service provider with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenServicesNull_ShouldThrowArgumentNullException()
    {
        IOptionsMonitor<FinancialOptions> monitor = Build(new()).Provider.GetRequiredService<IOptionsMonitor<FinancialOptions>>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MonetaryContextResolver(monitor, null!, logger: null);
        });

        Assert.AreEqual("services", ex.ParamName);
    }
}
