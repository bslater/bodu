// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Recurrence.Samples.CronExpressions.Scenarios;

namespace Bodu.Globalization.Recurrence.Samples.CronExpressions;

/// <summary>
/// Entry point for the cron sample: <c>CronExpression</c>, the Vixie five-field and optional-seconds
/// six-field forms — parsing and the <c>@</c> macros, next and previous occurrences, the Vixie
/// day-field semantics that distinguish this dialect from Quartz, canonical text and equality, and
/// the expressions that can never fire. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Recurrence.Samples.CronExpressions");
        Console.WriteLine("=====================================================");
        Console.WriteLine();

        CronBasics.Run();
        SecondsAndFormats.Run();
        VixieSemantics.Run();
        UnreachableAndDefects.Run();

        Console.WriteLine("Done.");
    }
}
