// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Recurrence.Samples.RecurrenceRules.Scenarios;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceRules;

/// <summary>
/// Entry point for the recurrence-rule sample: <c>RecurrenceRule</c>, the RFC 5545 <c>RRULE</c>
/// form — parsing and formatting, the <c>BY*</c> expansion and limiting semantics that
/// implementations most often get wrong, week numbering under <c>WKST</c>, the fluent
/// <c>RecurrenceRuleBuilder</c>, and bounded enumeration. Everything runs offline and
/// deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Recurrence.Samples.RecurrenceRules");
        Console.WriteLine("=====================================================");
        Console.WriteLine();

        RuleBasics.Run();
        ByPartSemantics.Run();
        WeekNumbering.Run();
        BuildingRules.Run();
        BoundedEnumeration.Run();

        Console.WriteLine("Done.");
    }
}
