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
        Console.WriteLine("--- Three-tier rounding: per-step vs deferred vs exact ---");

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

        Console.WriteLine($"Per-step  (Money<USD>)      : {perStep}");
        Console.WriteLine($"Deferred  (CalculatedMoney) : {settled}");
        Console.WriteLine($"Exact     (Fraction)        : {exact}");
        Console.WriteLine("Pick the tier by contract: settle ledger entries per-step, run multi-step");
        Console.WriteLine("calculations deferred, and use fractions when the factor itself must be exact.");
        Console.WriteLine();
    }
}
