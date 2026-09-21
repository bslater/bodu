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
        SampleConsole.Scenario(
            "Typed and runtime money, and the bridges between them",
            what: "Shows amounts in the compile-time-typed form and the runtime-currency form, converts in both "
                + "directions, and demonstrates what each one can and cannot check.",
            why: "Both forms are necessary and neither is sufficient. When the currency is known at compile time "
                + "the type parameter makes adding dollars to euros a build error, which is the strongest "
                + "guarantee available and worth having wherever it applies. But currency is often data - it "
                + "arrives in a request, a database row or a rate feed - and a generic type cannot be "
                + "parameterized on a runtime value, so the untyped form has to exist and has to check at run "
                + "time instead. The bridges are what keep this from splitting the codebase in two: a typed "
                + "amount can cross a boundary as runtime money and be recovered on the other side, with the "
                + "recovery checking that the currency is what the caller expected.",
            expect: "The typed form rejects a mismatched currency at compile time, which no output can show. "
                + "What the output does show is the runtime form catching the same mistake as a false return "
                + "rather than a wrong answer, and the bridges round-tripping an amount through the untyped form "
                + "and back without changing it.");

        // Money<TCurrency> encodes the currency in the type via an ICurrency tag, so mixing
        // currencies is a *compile* error, and construction rounds to the currency's minor units.
        Money<USD> price = Money.Of<USD>(19.995m);   // banker's rounding -> 20.00
        Money<USD> tax = price * 0.1m;               // 2.00
        Money<USD> total = price + tax;              // 22.00

        // Money<USD> wrong = total + Money.Of<JPY>(500m);   // does not compile - currencies differ

        Console.WriteLine($"  Typed total : {total}  (Money<{Money<USD>.IsoCode}>, {Money<USD>.MinorUnits} minor units)");

        // Widening to the runtime form is implicit and lossless: the ISO code travels in the value.
        Money runtime = total;
        Console.WriteLine($"  Runtime     : {runtime}  (Money, Code={runtime.Code})");

        // Narrowing back is checked. The explicit cast and As<T>() throw on a currency mismatch;
        // TryAs<T>() reports it instead. A wiring mistake surfaces at the boundary, not as a
        // silently mis-labelled amount.
        var back = (Money<USD>)runtime;
        Console.WriteLine($"  Cast back   : {back}");

        if (!runtime.TryAs<JPY>(out _))
            Console.WriteLine("  TryAs<JPY>  : false - the runtime value is USD, not JPY");

        Console.WriteLine();
    }
}
