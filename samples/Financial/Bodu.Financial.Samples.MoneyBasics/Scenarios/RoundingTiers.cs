// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RoundingTiers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Financial.Currencies;
using Bodu.Numerics;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates the three-tier rounding model that is the central design idea of the money types:
/// <see cref="Money{TCurrency}" /> rounds after every operation, <see cref="CalculatedMoney" /> defers
/// rounding to a single settlement, and <see cref="Fraction{T}" /> arithmetic is exact.
/// </summary>
public static class RoundingTiers
{
    /// <summary>
    /// Compounds the same balance twelve times under each tier and compares the totals.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Three-tier rounding - per-step, deferred, and exact",
            what: "Compounds the same balance monthly for a year three ways: with Money rounding after every "
                + "multiplication, with CalculatedMoney deferring to a single settlement, and with an exact "
                + "rational factor applied in one step.",
            why: "This is the central design idea of the money types, and the reason there are three of them "
                + "rather than one. Rounding after every operation is correct for a ledger, where each entry is "
                + "a real settled amount someone can be paid - but it is wrong for a calculation, because the "
                + "error compounds along with the balance. Deferring rounding to a single settlement is correct "
                + "for the calculation and wrong for the ledger, since intermediate values are not amounts "
                + "anybody holds. Exact rational arithmetic is for when the factor itself must not be "
                + "approximated: a monthly rate of 5 percent over 12 is a repeating decimal, so even before "
                + "rounding, a decimal factor has already lost something. Having all three as distinct types "
                + "means the choice is made once, visibly, rather than being an accident of where a Round call "
                + "happened to land.",
            expect: "Three different totals from the same input and the same nominal rate. The differences are "
                + "small and that is exactly the hazard - they are too small to notice in a test and large "
                + "enough to matter over a portfolio, which is why the type system rather than a convention "
                + "has to carry the decision.");

        // 5% p.a. compounded monthly on 10,000.00. The true monthly factor is 1205/1200; as a
        // decimal it is already truncated (1.00416666...), and that tiny error is separate from
        // the rounding the money types apply.
        Money<USD> principal = Money.Of<USD>(10_000.00m);
        var monthlyFactor = 1m + (0.05m / 12m);

        // Tier 1 - Money<T>: every multiplication rounds the running balance to USD's two minor
        // units before the next month compounds it. Fine for settled ledger amounts; over a chain
        // of operations the per-step rounding error accumulates.
        Money<USD> perStep = principal;
        for (var month = 0; month < 12; month++)
            perStep *= monthlyFactor;

        // Tier 2 - CalculatedMoney: the same chain, but the intermediate values keep full decimal
        // precision. Nothing is rounded until RoundToMoney settles the result once at the end.
        CalculatedMoney deferred = principal.ToCalculated();
        for (var month = 0; month < 12; month++)
            deferred *= monthlyFactor;
        Money settled = deferred.RoundToMoney(MidpointRounding.ToEven);

        // Tier 3 - exact fractions: MultiplyExact applies the true rational factor (1205/1200)^12
        // with BigInteger arithmetic, so there is no decimal truncation at all; the single rounding
        // happens when the product lands back in Money<USD>.
        var annualFactor = new Fraction<BigInteger>(
            BigInteger.Pow(1205, 12),
            BigInteger.Pow(1200, 12));
        Money<USD> exact = principal.MultiplyExact(annualFactor);

        Console.WriteLine($"  Per-step  (Money<USD>)      : {perStep}");
        Console.WriteLine($"  Deferred  (CalculatedMoney) : {settled}");
        Console.WriteLine($"  Exact     (Fraction)        : {exact}");
        Console.WriteLine("  (two cents apart on one balance over one year - too small to notice in a test, large enough to matter over a portfolio, which is why the type carries the decision)");

        Console.WriteLine("  Pick the tier by contract: settle ledger entries per-step, run multi-step");
        Console.WriteLine("  calculations deferred, and use fractions when the factor itself must be exact.");
        Console.WriteLine();
    }
}
