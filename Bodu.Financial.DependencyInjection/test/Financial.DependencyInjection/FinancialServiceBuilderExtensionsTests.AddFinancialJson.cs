// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.AddFinancialJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddFinancialJson</c> registers configured serializer options resolvable by key.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenRegistered_ShouldProvideConfiguredOptions()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial()
            .AddFinancialJson(Serialization.FinancialJsonPolicy.Compact)
            .Services.BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, "USD"), options);

        Assert.AreEqual("\"19.99 USD\"", json);
    }
}
