// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyScenario.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates <see cref="CalculatedMoney" /> as the deferred-arithmetic tier: a full-precision
/// <c>decimal</c> carrier that accumulates a multi-step calculation without rounding, then settles
/// to a <see cref="Money" /> exactly once via <c>RoundToMoney</c>. Where <see cref="Money{TCurrency}" />
/// rounds after every operation, <see cref="CalculatedMoney" /> rounds only at materialization — so the
/// single rounding decision (and which way the leftover half-cent falls) is explicit.
/// </summary>
public static class CalculatedMoneyScenario
{
    /// <summary>
    /// Builds a shared bill through unrounded steps, splits it, and materializes each share once.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CalculatedMoney: deferred arithmetic, settle once ---");

        // Three line items enter the calculation as full-precision carriers. Adding CalculatedMoney
        // values keeps every digit - nothing is rounded to cents yet.
        CalculatedMoney bill =
            new CalculatedMoney(9.99m, CurrencyCode.USD)
            + new CalculatedMoney(9.99m, CurrencyCode.USD)
            + new CalculatedMoney(6.95m, CurrencyCode.USD);

        // Split the bill two ways. The exact half is 13.465 - a half-cent that has no representation
        // in USD's two minor units. CalculatedMoney carries it unrounded until settlement.
        CalculatedMoney perPerson = bill / 2m;

        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Bill total (unrounded): {0} USD", bill.Amount));
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Split 2 ways (unrounded): {0} USD", perPerson.Amount));

        // Materialize once. The default settles with banker's rounding; an explicit MidpointRounding
        // overrides it - the same deferred value, two documented ways to break the half-cent tie.
        Money banker = perPerson.RoundToMoney();
        Money awayFromZero = perPerson.RoundToMoney(MidpointRounding.AwayFromZero);

        Console.WriteLine($"Settle (banker's)     : {banker}");
        Console.WriteLine($"Settle (away-from-0)  : {awayFromZero}");
        Console.WriteLine("13.465 is a half-cent midpoint: banker's takes the even neighbour, away-from-zero rounds up.");

        Console.WriteLine();
    }
}
