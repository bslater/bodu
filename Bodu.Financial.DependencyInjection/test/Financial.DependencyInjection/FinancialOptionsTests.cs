// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies <see cref="FinancialOptions" />: its defaults and how <c>AddFinancialService</c> binds it from
/// configuration.
/// </summary>
[TestClass]
public sealed partial class FinancialOptionsTests
{
    /// <summary>
    /// Binds <see cref="FinancialOptions" /> through <c>AddFinancialService</c> from a configuration over the specified
    /// settings.
    /// </summary>
    /// <param name="settings">The configuration settings, keyed by path.</param>
    /// <returns>The bound options.</returns>
    private static FinancialOptions Bind(Dictionary<string, string?> settings)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceProvider provider = new ServiceCollection().AddFinancialService(configuration).Services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<FinancialOptions>>().Value;
    }
}
