// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormattingParsing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.MoneyBasics.Scenarios;

/// <summary>
/// Demonstrates the shared format-specifier vocabulary, the fluent <see cref="MoneyFormatterBuilder" />,
/// and the four <see cref="MoneyParseMode" /> parsing strictness levels — including the invariant
/// round-trip form that survives storage and transport losslessly.
/// </summary>
public static class FormattingParsing
{
    /// <summary>
    /// Formats one amount under each specifier, builds a custom formatter, and parses several inputs.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Formatting and parsing ---");

        // Formatting is culture-explicit here so the sample prints the same on every machine.
        var enUS = new CultureInfo("en-US");
        Money money = Money.From(1234.56m, CurrencyCode.USD);

        // The specifier vocabulary is shared by Money and Money<TCurrency> (case-insensitive).
        Console.WriteLine($"G  (default)     : {money.ToString("G", enUS)}");   // ISO + grouped number
        Console.WriteLine($"C  (currency)    : {money.ToString("C", enUS)}");   // culture-native symbol
        Console.WriteLine($"L  (long name)   : {money.ToString("L", enUS)}");   // English currency name
        Console.WriteLine($"N  (number only) : {money.ToString("N", enUS)}");   // no currency designator
        Console.WriteLine($"C0 (0 decimals)  : {money.ToString("C0", enUS)}");  // precision override
        Console.WriteLine($"~C (elide match) : {money.ToString("~C", enUS)}");  // symbol elided when culture matches

        // MoneyFormatterBuilder captures a reusable display policy once instead of scattering
        // format strings through the code base.
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithEnglishName()
            .WithCulture(enUS)
            .Build();
        Console.WriteLine($"Formatter        : {formatter.Format(money)}");

        // "R" is the invariant round-trip form: culture-independent, no grouping, natural precision.
        // Pair it with RoundTripOnly parsing for storage and wire formats.
        var wire = money.ToString("R");
        Money restored = Money.Parse(wire, new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly });
        Console.WriteLine($"R round-trip     : \"{wire}\" -> {restored} (equal: {restored == money})");

        // StrictIso (the default) accepts only canonical "<ISO> <amount>" / "<amount> <ISO>" forms;
        // LenientImport additionally trims white space and upcases the ISO code - for ingesting
        // spreadsheets and external feeds.
        Money<USD> typed = Money<USD>.Parse("USD 99.95", enUS);
        Money imported = Money.Parse("  1234.56 usd ", new MoneyParseOptions { Mode = MoneyParseMode.LenientImport });
        Console.WriteLine($"StrictIso        : \"USD 99.95\" -> {typed}");
        Console.WriteLine($"LenientImport    : \"  1234.56 usd \" -> {imported}");

        // TryParse reports malformed input instead of throwing.
        if (!Money.TryParse("not money", enUS, out _))
            Console.WriteLine("TryParse         : \"not money\" -> false");

        Console.WriteLine();
    }
}
