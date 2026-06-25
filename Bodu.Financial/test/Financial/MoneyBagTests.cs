// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyBag" /> aggregation, conversion to a single target currency, equality, enumeration,
/// and JSON serialisation.
/// </summary>
[TestClass]
public partial class MoneyBagTests
{
    /// <summary>
    /// Verifies that <see cref="MoneyBag.Empty" /> has no balances.
    /// </summary>
    [TestMethod]
    public void Empty_WhenAccessed_ShouldHaveNoBalances()
    {
        Assert.IsTrue(MoneyBag.Empty.IsEmpty);
        Assert.AreEqual(0, MoneyBag.Empty.Count);
    }

    /// <summary>
    /// Verifies that the constructor sums same-currency entries and prunes zero balances.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGivenBalances_ShouldSumByCurrencyAndPruneZeros()
    {
        Money[] entries =
        [
            new(100m, CurrencyCode.USD),
            new(50m, CurrencyCode.EUR),
            new(-100m, CurrencyCode.USD),    // cancels the USD entry
            new(25m, CurrencyCode.EUR),      // sums with the prior EUR entry
        ];

        var bag = new MoneyBag(entries);

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(new Money(75m, CurrencyCode.EUR), bag.GetBalance(CurrencyCode.EUR));
        Assert.IsNull(bag.GetBalance(CurrencyCode.USD));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.Add(Money)" /> creates a new bag rather than mutating.
    /// </summary>
    [TestMethod]
    public void Add_WhenCalled_ShouldReturnNewBagWithoutMutatingOriginal()
    {
        MoneyBag start = MoneyBag.Empty;

        MoneyBag added = start.Add(new Money(100m, CurrencyCode.USD));

        Assert.AreNotEqual(start, added);
        Assert.IsTrue(start.IsEmpty);
        Assert.AreEqual(1, added.Count);
    }

    /// <summary>
    /// Verifies that adding the typed and runtime-tagged amount produce the same result.
    /// </summary>
    [TestMethod]
    public void Add_WhenTypedAndRuntimeTagged_ShouldYieldEquivalentBags()
    {
        MoneyBag typedAdded = MoneyBag.Empty.Add(new Money<USD>(100m));
        MoneyBag runtimeAdded = MoneyBag.Empty.Add(new Money(100m, CurrencyCode.USD));

        Assert.AreEqual(typedAdded, runtimeAdded);
    }

    /// <summary>
    /// Verifies that adding cancels out an existing balance to remove the entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenCancellingExistingBalance_ShouldRemoveCurrencyEntry()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(100m, CurrencyCode.USD))
            .Add(new Money(-100m, CurrencyCode.USD));

        Assert.IsTrue(bag.IsEmpty);
    }

    /// <summary>
    /// Verifies that adding a zero-amount value is a no-op.
    /// </summary>
    [TestMethod]
    public void Add_WhenAmountIsZero_ShouldReturnUnchangedBag()
    {
        MoneyBag start = MoneyBag.Empty.Add(new Money(100m, CurrencyCode.USD));

        MoneyBag after = start.Add(Money.Zero(CurrencyCode.EUR));

        Assert.AreEqual(start, after);
    }

    /// <summary>
    /// Verifies that subtraction is the inverse of addition.
    /// </summary>
    [TestMethod]
    public void Subtract_WhenAppliedToSameCurrency_ShouldReduceBalance()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(100m, CurrencyCode.USD))
            .Subtract(new Money(30m, CurrencyCode.USD));

        Assert.AreEqual(new Money(70m, CurrencyCode.USD), bag.GetBalance(CurrencyCode.USD));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.Combine" /> sums per-currency balances across two bags.
    /// </summary>
    [TestMethod]
    public void Combine_WhenTwoBagsShareCurrency_ShouldSumPerCurrency()
    {
        var left = new MoneyBag([new Money(100m, CurrencyCode.USD), new Money(50m, CurrencyCode.EUR)]);
        var right = new MoneyBag([new Money(25m, CurrencyCode.USD), new Money(75m, CurrencyCode.JPY)]);

        MoneyBag combined = left.Combine(right);

        Assert.AreEqual(new Money(125m, CurrencyCode.USD), combined.GetBalance(CurrencyCode.USD));
        Assert.AreEqual(new Money(50m, CurrencyCode.EUR), combined.GetBalance(CurrencyCode.EUR));
        Assert.AreEqual(new Money(75m, CurrencyCode.JPY), combined.GetBalance(CurrencyCode.JPY));
    }

    /// <summary>
    /// Verifies that combining a bag with empty returns the same instance.
    /// </summary>
    [TestMethod]
    public void Combine_WhenOtherIsEmpty_ShouldReturnSameBag()
    {
        MoneyBag left = MoneyBag.Empty.Add(new Money(100m, CurrencyCode.USD));

        Assert.AreSame(left, left.Combine(MoneyBag.Empty));
    }

    /// <summary>
    /// Verifies that GetBalance returns null for absent currencies.
    /// </summary>
    [TestMethod]
    public void GetBalance_WhenAbsent_ShouldReturnNull()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money(100m, CurrencyCode.USD));

        Assert.IsNull(bag.GetBalance(CurrencyCode.EUR));
    }

    /// <summary>
    /// Verifies typed GetBalance.
    /// </summary>
    [TestMethod]
    public void GetBalance_WhenTypedAndPresent_ShouldReturnTypedMoney()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money<USD>(100m));

        Money<USD>? balance = bag.GetBalance<USD>();

        Assert.IsNotNull(balance);
        Assert.AreEqual(new Money<USD>(100m), balance.Value);
    }

    /// <summary>
    /// Verifies that enumeration yields entries in ISO-code lexicographic order.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenIterated_ShouldYieldEntriesInIsoOrder()
    {
        var bag = new MoneyBag(
        [
            new Money(1m, CurrencyCode.USD),
            new Money(2m, CurrencyCode.EUR),
            new Money(3m, CurrencyCode.AUD),
            new Money(4m, CurrencyCode.JPY),
        ]);

        var codes = bag.Select(v => v.Code.ToString()).ToList();

        Assert.HasCount(4, codes);
        Assert.AreEqual("AUD", codes[0]);
        Assert.AreEqual("EUR", codes[1]);
        Assert.AreEqual("JPY", codes[2]);
        Assert.AreEqual("USD", codes[3]);
    }

    /// <summary>
    /// Verifies bag equality.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameBalancesInDifferentInsertionOrder_ShouldReturnTrue()
    {
        MoneyBag a = MoneyBag.Empty
            .Add(new Money(100m, CurrencyCode.USD))
            .Add(new Money(50m, CurrencyCode.EUR));
        MoneyBag b = MoneyBag.Empty
            .Add(new Money(50m, CurrencyCode.EUR))
            .Add(new Money(100m, CurrencyCode.USD));

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that operators combine bags and add/subtract Money values.
    /// </summary>
    [TestMethod]
    public void Operators_WhenChained_ShouldProduceExpectedComposition()
    {
        MoneyBag bag = MoneyBag.Empty
            + new Money(100m, CurrencyCode.USD)
            + new Money(50m, CurrencyCode.EUR)
            - new Money(25m, CurrencyCode.USD);

        Assert.AreEqual(new Money(75m, CurrencyCode.USD), bag.GetBalance(CurrencyCode.USD));
        Assert.AreEqual(new Money(50m, CurrencyCode.EUR), bag.GetBalance(CurrencyCode.EUR));
    }

    /// <summary>
    /// Verifies conversion via an <see cref="IExchangeRateProvider" /> aggregates correctly.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenUsingRateProvider_ShouldAggregateToTargetCurrency()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<USD>(100m))
            .Add(new Money<EUR>(50m))
            .Add(new Money<JPY>(10000m));

        Dictionary<(string From, string To), decimal> rates = new()
        {
            { ("EUR", "USD"), 1.10m },
            { ("JPY", "USD"), 0.0067m },
        };
        var table = new FixedExchangeRateTable(rates);

        Money<USD> total = bag.ConvertTo<USD>(table);

        // 100 + 50×1.10 + 10000×0.0067 = 100 + 55 + 67 = 222
        Assert.AreEqual(new Money<USD>(222m), total);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.ConvertTo{TTarget}(Func{string, string, decimal})" /> works with a
    /// delegate-based lookup.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenUsingDelegateLookup_ShouldAggregateToTargetCurrency()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<USD>(50m))
            .Add(new Money<EUR>(50m));

        Money<USD> total = bag.ConvertTo<USD>((from, to) =>
            (from, to) switch
            {
                ("EUR", "USD") => 1.10m,
                _ => 1m,
            });

        Assert.AreEqual(new Money<USD>(105m), total);
    }

    /// <summary>
    /// Verifies that <see cref="FixedExchangeRateTable" /> uses the inverse rate when the direct pair is missing.
    /// </summary>
    [TestMethod]
    public void FixedRateTable_WhenInverseDefined_ShouldUseInverseRate()
    {
        Dictionary<(string From, string To), decimal> rates = new()
        {
            { ("USD", "EUR"), 0.92m },
        };
        var table = new FixedExchangeRateTable(rates);

        decimal usdToEur = table.GetRate("USD", "EUR");
        decimal eurToUsd = table.GetRate("EUR", "USD");

        Assert.AreEqual(0.92m, usdToEur);
        Assert.AreEqual(1m / 0.92m, eurToUsd);
    }

    /// <summary>
    /// Verifies that the rate table returns 1 for same-currency lookups without touching the dictionary.
    /// </summary>
    [TestMethod]
    public void FixedRateTable_WhenSameCurrency_ShouldReturnOne()
    {
        var table = new FixedExchangeRateTable(new Dictionary<(string, string), decimal>());

        Assert.AreEqual(1m, table.GetRate("USD", "USD"));
    }

    /// <summary>
    /// Verifies that the rate table throws <see cref="KeyNotFoundException" /> for missing pairs.
    /// </summary>
    [TestMethod]
    public void FixedRateTable_WhenPairAbsent_ShouldThrowKeyNotFoundException()
    {
        var table = new FixedExchangeRateTable(new Dictionary<(string, string), decimal>());

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = table.GetRate("USD", "EUR");
        });
    }

    /// <summary>
    /// Verifies the JSON shape and round-trip preservation.
    /// </summary>
    [TestMethod]
    public void Json_WhenRoundTripped_ShouldPreserveBag()
    {
        MoneyBag original = MoneyBag.Empty
            .Add(new Money(100m, CurrencyCode.USD))
            .Add(new Money(50m, CurrencyCode.EUR));

        string json = JsonSerializer.Serialize(original);
        MoneyBag? recovered = JsonSerializer.Deserialize<MoneyBag>(json);

        Assert.IsNotNull(recovered);
        Assert.AreEqual(original, recovered);
        StringAssert.Contains(json, "\"balances\"");
        StringAssert.Contains(json, "\"USD\":100");
    }

    /// <summary>
    /// Verifies that the typed <see cref="MoneyBag.Subtract{TCurrency}(Money{TCurrency})" /> overload reduces the
    /// matching currency's balance.
    /// </summary>
    [TestMethod]
    public void Subtract_WhenTypedAmount_ShouldReduceBalance()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money(100m, CurrencyCode.USD)).Subtract(new Money<USD>(25m));

        Assert.AreEqual(new Money(75m, CurrencyCode.USD), bag.GetBalance(CurrencyCode.USD));
    }

    /// <summary>
    /// Verifies that the bag-plus-bag operator sums per-currency balances and drops a currency whose combined balance
    /// nets to zero.
    /// </summary>
    [TestMethod]
    public void OperatorPlus_WhenCombiningTwoBags_ShouldSumAndCancelToZero()
    {
        var left = new MoneyBag([new Money(10m, CurrencyCode.USD), new Money(5m, CurrencyCode.EUR)]);
        var right = new MoneyBag([new Money(-10m, CurrencyCode.USD), new Money(3m, CurrencyCode.EUR)]);

        MoneyBag combined = left + right;

        Assert.IsNull(combined.GetBalance(CurrencyCode.USD));
        Assert.AreEqual(new Money(8m, CurrencyCode.EUR), combined.GetBalance(CurrencyCode.EUR));
    }

    /// <summary>
    /// Verifies that enumerating a bag through the non-generic <see cref="System.Collections.IEnumerable" /> surface
    /// yields its balances.
    /// </summary>
    [TestMethod]
    public void NonGenericEnumeration_ShouldYieldBalances()
    {
        System.Collections.IEnumerable bag = new MoneyBag([new Money(1m, CurrencyCode.USD)]);

        var items = new List<object>();
        foreach (object? item in bag)
            items.Add(item);

        Assert.HasCount(1, items);
        Assert.AreEqual(new Money(1m, CurrencyCode.USD), (Money)items[0]);
    }

    /// <summary>
    /// Verifies that the <see cref="MoneyBagConversionRoundingPolicy.RoundEachCurrencyThenSum" /> policy rounds each
    /// converted balance to the target precision before summing, and passes the target-currency balance through
    /// without conversion.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenRoundEachCurrencyThenSum_ShouldRoundPerCurrencyBeforeSumming()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<USD>(5m))
            .Add(new Money<EUR>(2m))
            .Add(new Money<JPY>(100m));

        Money<USD> total = bag.ConvertTo<USD>(
            (from, to) => (from, to) switch
            {
                ("EUR", "USD") => 1.10m,
                ("JPY", "USD") => 0.01m,
                _ => 1m,
            },
            MoneyBagConversionRoundingPolicy.RoundEachCurrencyThenSum);

        // USD 5 (pass-through) + EUR 2×1.10 = 2.20 + JPY 100×0.01 = 1.00 → 8.20.
        Assert.AreEqual(new Money<USD>(8.20m), total);
    }
}
