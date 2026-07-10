// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonPolicies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates the three <see cref="FinancialJsonPolicy" /> serialization policies registered through
/// <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" />: the canonical
/// <c>Strict</c> object shape, the forgiving <c>Lenient</c> import shape, and the <c>Compact</c> string shape.
/// </summary>
public static class JsonPolicies
{
    /// <summary>
    /// Serializes and deserializes money values under each policy.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- JSON policies: Strict, Lenient, Compact ---");

        Money<USD> price = Money.Of<USD>(19.99m);
        MoneyBag bag = MoneyBag.Of(Money.From(19.99m, CurrencyCode.USD), Money.From(12.34m, CurrencyCode.EUR));

        // Strict (default): the canonical {"amount":..,"currency":".."} object shape for ledgers,
        // persistence, and audit data. Duplicate properties and mismatched currencies are rejected.
        var strict = new JsonSerializerOptions().AddFinancialJsonConverters();
        var ledgerJson = JsonSerializer.Serialize(price, strict);
        Money<USD> restored = JsonSerializer.Deserialize<Money<USD>>(ledgerJson, strict);
        Console.WriteLine($"Strict  : {ledgerJson} -> {restored}");

        // Compact: money as a single "amount ISO" string, bags as a flat { "ISO": amount } map -
        // for APIs and logs where the object shape is too heavy.
        var compact = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Compact);
        Console.WriteLine($"Compact : {JsonSerializer.Serialize(price, compact)}");
        Console.WriteLine($"Compact : {JsonSerializer.Serialize(bag, compact)}  (MoneyBag)");

        // Lenient: same shape as Strict, but trims whitespace and upcases lowercase ISO codes -
        // for ingesting external feeds. Not a canonical storage shape.
        var lenient = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Lenient);
        Money imported = JsonSerializer.Deserialize<Money>("""{"amount":12.34,"currency":"usd"}""", lenient);
        Console.WriteLine($"Lenient : lowercase \"usd\" accepted -> {imported}");

        Console.WriteLine();
    }
}
