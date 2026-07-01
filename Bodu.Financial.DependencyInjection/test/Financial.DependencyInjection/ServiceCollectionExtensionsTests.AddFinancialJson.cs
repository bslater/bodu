// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.AddFinancialJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that an explicit <c>AddFinancialJson</c> overrides a JSON policy already bound from configuration,
    /// because it is registered last under the same key.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenCalledAfterBoundPolicy_ShouldOverrideRegisteredJsonOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Financial:JsonPolicy"] = nameof(FinancialJsonPolicy.Compact),
            })
            .Build();

        ServiceProvider provider = new ServiceCollection()
            .AddFinancialService(configuration)
            .AddFinancialJson(FinancialJsonPolicy.Strict)
            .Services.BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreNotEqual("\"19.99 USD\"", json, "the explicit Strict policy should override the bound Compact policy");
    }
}
