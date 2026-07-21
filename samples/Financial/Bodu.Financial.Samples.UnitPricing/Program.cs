// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Samples.UnitPricing.Scenarios;

namespace Bodu.Financial.Samples.UnitPricing;

/// <summary>
/// Entry point for the unit-pricing sample: carrying prices at a higher precision than a currency's two minor units
/// (a six-decimal-place share price), and serializing and deserializing them so the extra decimal places survive the
/// round-trip. Everything runs offline with in-code data only.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.UnitPricing");
        Console.WriteLine("==================================");
        Console.WriteLine();

        ExplicitScaleMoney.Run();
        CalculatedUnitPrice.Run();
        PriceListDocument.Run();

        Console.WriteLine("Done.");
    }
}
