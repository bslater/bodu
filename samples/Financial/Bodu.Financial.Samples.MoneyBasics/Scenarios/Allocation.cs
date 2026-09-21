// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Allocation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates sum-preserving allocation (the largest-remainder method) and cash rounding.
/// Splitting money by naive division loses or invents cents; <see cref="Money{TCurrency}.Allocate(int)" />
/// guarantees the parts always re-total to the original amount.
/// </summary>
public static class Allocation
{
    /// <summary>
    /// Splits amounts into equal and weighted parts and snaps a price to cash denominations.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Allocation - splits that always sum back, and cash rounding",
            what: "Splits an invoice three equal ways and then by weights, splits a zero-decimal currency, and "
                + "rounds an electronic amount to the physical cash increment its currency declares.",
            why: "Dividing money is not dividing a number. A hundred dollars split three ways is not three lots "
                + "of 33.33, because that is 99.99 and the missing cent has to belong to someone - so the "
                + "operation distributes the remainder rather than rounding each part independently. The "
                + "guarantee is that the parts sum back to the original exactly and differ by at most one minor "
                + "unit, which is what makes allocation safe to use on anything that has to reconcile. The "
                + "currency's own shape matters too: a zero-decimal currency has no minor unit to split, and "
                + "some currencies no longer mint their smallest coin, so a physical payment rounds to an "
                + "increment that electronic settlement does not.",
            expect: "Every split sums back to the original amount, which a naive per-part rounding would not. "
                + "The zero-decimal currency allocates in whole units because it has nothing smaller. The cash "
                + "rounding moves the amount to the increment declared by the currency itself rather than to a "
                + "hardcoded one - it is a property of the currency, not of the call site.");

        // Equal split. 100.00 / 3 is 33.333... - naive rounding gives 3 x 33.33 = 99.99 and a lost
        // cent. Allocate distributes the remainder cent-by-cent (largest remainder first), so the
        // parts differ by at most one minor unit and always sum back exactly.
        Money<USD> invoice = Money.Of<USD>(100.00m);
        Money<USD>[] equal = invoice.Allocate(3);
        Console.WriteLine($"  {invoice} into 3     : {string.Join(", ", equal)}  (sum {equal.Aggregate((a, b) => a + b)})");

        // Weighted split: the ratios need not be normalized - 5:3:2 works as well as 0.5/0.3/0.2.
        Money<USD>[] weighted = invoice.Allocate([0.5m, 0.3m, 0.2m]);
        Console.WriteLine($"  {invoice} at 50/30/20: {string.Join(", ", weighted)}");

        // Zero-decimal currencies allocate in whole units - JPY has no minor units to split.
        Money<JPY> yen = Money.Of<JPY>(1000m);
        Console.WriteLine($"  {yen} into 3    : {string.Join(", ", yen.Allocate(3))}");

        // Cash rounding is a separate, per-currency policy: CHF electronic amounts keep two decimals,
        // but physical cash snaps to the 0.05 increment declared by the CHF currency tag.
        Money<CHF> card = Money.Of<CHF>(7.02m);
        Console.WriteLine($"  {card} cash      : {card.RoundToCash()}  (CHF cash increment {Money<CHF>.CashRoundingIncrement})");

        Console.WriteLine();
    }
}
