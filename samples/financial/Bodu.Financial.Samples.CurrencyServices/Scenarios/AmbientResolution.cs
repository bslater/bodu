// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmbientResolution.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.CurrencyServices.Scenarios;

/// <summary>
/// Demonstrates the ambient currency-resolution seam. Runtime <see cref="Money" /> resolves currency
/// metadata (minor units, names, parse validation) through <see cref="CurrencyResolution.Current" />;
/// <see cref="CurrencyResolution.PushScoped" /> swaps the lookup for a scope (async-flow safe — the
/// test seam), while <see cref="CurrencyResolution.SetDefault" /> is the one-time composition-root
/// promotion (see the host scenario).
/// </summary>
public static class AmbientResolution
{
    /// <summary>
    /// Queries the default lookup, then restricts it for a scope and shows the effect on parsing.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Ambient currency resolution ---");

        // The default lookup serves the full ISO 4217 registry.
        ICurrencyLookup current = CurrencyResolution.Current;
        if (current.TryByIsoCode("AUD", out CurrencyInfo aud))
            Console.WriteLine($"AUD: {aud.EnglishName}, {aud.MinorUnits} minor units, cash increment {aud.CashRoundingIncrement}");

        // Runtime parsing consults the ambient lookup, so any registered ISO code is accepted.
        Console.WriteLine($"Parse \"THB 25.00\" (default lookup)   : {Money.Parse("THB 25.00", CultureInfo.InvariantCulture)}");

        // Scope a restricted lookup over the settlement currencies this system supports. Inside the
        // scope, unsupported codes fail resolution everywhere the ambient seam is consulted.
        using (CurrencyResolution.PushScoped(new RestrictedCurrencyLookup(current, "AUD", "USD", "EUR")))
        {
            Console.WriteLine($"Parse \"USD 10.00\" (restricted scope) : {Money.Parse("USD 10.00", CultureInfo.InvariantCulture)}");

            if (!Money.TryParse("THB 25.00", CultureInfo.InvariantCulture, out _))
                Console.WriteLine("Parse \"THB 25.00\" (restricted scope) : rejected - not an allowed currency");
        }

        // Disposing the scope restores the previous lookup.
        Console.WriteLine($"Parse \"THB 25.00\" (scope disposed)   : {Money.Parse("THB 25.00", CultureInfo.InvariantCulture)}");

        Console.WriteLine();
    }
}
