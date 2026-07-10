// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TypedRuntimeBridges.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates the duality between the compile-time-safe <see cref="Money{TCurrency}" /> and the
/// runtime-tagged <see cref="Money" />, and the bridges between them. Use the typed form where the
/// domain fixes the currency; use the runtime form where the currency arrives as data.
/// </summary>
public static class TypedRuntimeBridges
{
    /// <summary>
    /// Builds a typed total, crosses to the runtime form and back, and shows the checked bridges.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Typed vs runtime money, and the bridges between them ---");

        // Money<TCurrency> encodes the currency in the type via an ICurrency tag, so mixing
        // currencies is a *compile* error, and construction rounds to the currency's minor units.
        Money<USD> price = Money.Of<USD>(19.995m);   // banker's rounding -> 20.00
        Money<USD> tax = price * 0.1m;               // 2.00
        Money<USD> total = price + tax;              // 22.00

        // Money<USD> wrong = total + Money.Of<JPY>(500m);   // does not compile - currencies differ

        Console.WriteLine($"Typed total : {total}  (Money<{Money<USD>.IsoCode}>, {Money<USD>.MinorUnits} minor units)");

        // Widening to the runtime form is implicit and lossless: the ISO code travels in the value.
        Money runtime = total;
        Console.WriteLine($"Runtime     : {runtime}  (Money, Code={runtime.Code})");

        // Narrowing back is checked. The explicit cast and As<T>() throw on a currency mismatch;
        // TryAs<T>() reports it instead. A wiring mistake surfaces at the boundary, not as a
        // silently mis-labelled amount.
        var back = (Money<USD>)runtime;
        Console.WriteLine($"Cast back   : {back}");

        if (!runtime.TryAs<JPY>(out _))
            Console.WriteLine("TryAs<JPY>  : false - the runtime value is USD, not JPY");

        Console.WriteLine();
    }
}
