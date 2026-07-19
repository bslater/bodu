// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Samples.CurrencyServices.Scenarios;

namespace Bodu.Financial.Samples.CurrencyServices;

/// <summary>
/// Entry point for the currency-services sample: the ambient currency-resolution seam, named
/// monetary contexts, and the <c>AddFinancialService</c> host wiring.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.CurrencyServices");
        Console.WriteLine("=======================================");
        Console.WriteLine();

        CurrencyCatalogue.Run();
        AmbientResolution.Run();
        NamedContexts.Run();
        FinancialServiceHost.Run();

        Console.WriteLine("Done.");
    }
}
