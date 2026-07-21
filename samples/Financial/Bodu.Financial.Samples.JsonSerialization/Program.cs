// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Samples.JsonSerialization.Scenarios;

namespace Bodu.Financial.Samples.JsonSerialization;

/// <summary>
/// Entry point for the financial-JSON sample: the <c>Bodu.Financial.Serialization.Json</c> companion —
/// registering the converters on a <c>JsonSerializerOptions</c>, round-tripping <c>Money</c>,
/// <c>Money&lt;TCurrency&gt;</c>, <c>MoneyBag</c>, <c>ExchangeRate</c>, and <c>CurrencyPair</c>, the three
/// <c>FinancialJsonPolicy</c> wire shapes, and the keyed dependency-injection registration. Everything runs
/// offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Financial.Samples.JsonSerialization");
        Console.WriteLine("========================================");
        Console.WriteLine();

        RegisterConverters.Run();
        ExchangeRateJson.Run();
        PolicyShapes.Run();
        DiKeyedOptions.Run();

        Console.WriteLine("Done.");
    }
}
