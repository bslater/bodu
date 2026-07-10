// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonServiceCollectionExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Verifies the <see cref="FinancialJsonServiceCollectionExtensions.AddFinancialJson" /> dependency-injection
/// registration: the keyed <see cref="JsonSerializerOptions" /> resolution, last-registration-wins override
/// semantics, and argument validation.
/// </summary>
[TestClass]
public sealed class FinancialJsonServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that <c>AddFinancialJson</c> registers configured serializer options resolvable by
    /// <see cref="FinancialJsonServiceCollectionExtensions.JsonOptionsKey" />.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenRegistered_ShouldProvideConfiguredOptions()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddFinancialJson(FinancialJsonPolicy.Compact)
            .BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreEqual("\"19.99 USD\"", json);
    }

    /// <summary>
    /// Verifies that the parameterless registration defaults to the Strict object shape.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenPolicyOmitted_ShouldDefaultToStrict()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddFinancialJson()
            .BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that a later <c>AddFinancialJson</c> call overrides an earlier one, because the last registration
    /// under the same key wins.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenCalledTwice_ShouldUseLastRegisteredPolicy()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddFinancialJson(FinancialJsonPolicy.Compact)
            .AddFinancialJson(FinancialJsonPolicy.Strict)
            .BuildServiceProvider();

        JsonSerializerOptions options = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreNotEqual("\"19.99 USD\"", json, "the later Strict policy should override the earlier Compact policy");
        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that a null service collection is rejected.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenServicesNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = FinancialJsonServiceCollectionExtensions.AddFinancialJson(null!);
        });
    }

    /// <summary>
    /// Verifies that an undefined policy value is rejected.
    /// </summary>
    [TestMethod]
    public void AddFinancialJson_WhenPolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ServiceCollection().AddFinancialJson((FinancialJsonPolicy)999);
        });
    }
}
