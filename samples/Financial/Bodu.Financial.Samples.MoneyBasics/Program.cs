// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Samples.MoneyBasics.Scenarios;

namespace Bodu.Financial.Samples.MoneyBasics;

/// <summary>
/// Entry point for the money-basics sample: the core <c>Bodu.Financial</c> value types and policies,
/// exercised offline with in-code data only.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.MoneyBasics");
        Console.WriteLine("==================================");
        Console.WriteLine();

        RoundingTiers.Run();
        CalculatedMoneyScenario.Run();
        TypedRuntimeBridges.Run();
        Allocation.Run();
        FormattingParsing.Run();
        MoneyBagLedger.Run();
        JsonPolicies.Run();

        Console.WriteLine("Done.");
    }
}
