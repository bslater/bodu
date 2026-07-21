// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RegisterConverters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;

namespace Bodu.Financial.Samples.JsonSerialization.Scenarios;

/// <summary>
/// Demonstrates the single registration call that makes the serialization-agnostic core types
/// round-trip: <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" /> adds the
/// converters for <see cref="Money" />, <see cref="Money{TCurrency}" />, and <see cref="MoneyBag" /> onto a
/// fresh <see cref="JsonSerializerOptions" />, after which each value serializes and re-reads to an equal value.
/// </summary>
public static class RegisterConverters
{
    /// <summary>
    /// Registers the converters once, then serializes and deserializes each monetary type.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Register converters: Money, Money<TCurrency>, MoneyBag ---");

        // The core Bodu.Financial types carry no [JsonConverter] attribute - the library is
        // serialization-agnostic - so this one call is required before any of them round-trips.
        // With no policy argument it defaults to FinancialJsonPolicy.Strict, the canonical object shape.
        var options = new JsonSerializerOptions().AddFinancialJsonConverters();

        // Money<USD>: a compile-time-typed amount. Strict emits {"amount":..,"currency":".."}.
        Money<USD> typed = Money.Of<USD>(19.99m);
        var typedJson = JsonSerializer.Serialize(typed, options);
        Money<USD> typedBack = JsonSerializer.Deserialize<Money<USD>>(typedJson, options);
        Console.WriteLine($"Money<USD> : {typedJson}");
        Console.WriteLine($"           -> {typedBack} (round-trips equal: {typed == typedBack})");

        // Money: a runtime-typed amount carrying its CurrencyCode. Same Strict object shape.
        Money runtime = Money.From(12.34m, CurrencyCode.EUR);
        var runtimeJson = JsonSerializer.Serialize(runtime, options);
        Money runtimeBack = JsonSerializer.Deserialize<Money>(runtimeJson, options);
        Console.WriteLine($"Money      : {runtimeJson}");
        Console.WriteLine($"           -> {runtimeBack} (round-trips equal: {runtime == runtimeBack})");

        // MoneyBag: a multi-currency purse. Strict emits an object with a "balances" map.
        MoneyBag bag = MoneyBag.Of(Money.From(19.99m, CurrencyCode.USD), Money.From(12.34m, CurrencyCode.EUR));
        var bagJson = JsonSerializer.Serialize(bag, options);
        MoneyBag bagBack = JsonSerializer.Deserialize<MoneyBag>(bagJson, options)!;
        bagBack.TryGetBalance(CurrencyCode.USD, out Money usd);
        bagBack.TryGetBalance(CurrencyCode.EUR, out Money eur);
        Console.WriteLine($"MoneyBag   : {bagJson}");
        Console.WriteLine($"           -> USD {usd.Amount}, EUR {eur.Amount}");

        Console.WriteLine();
    }
}
