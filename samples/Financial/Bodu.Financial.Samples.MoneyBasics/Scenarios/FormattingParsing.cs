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
        SampleConsole.Scenario(
            "Formatting and parsing money",
            what: "Formats one amount under each format specifier - default, currency symbol, long name, number "
                + "only, fixed decimals, and the elide-matching-code form - then parses text back under the "
                + "round-trip, strict-ISO and lenient-import policies, including input that is rejected.",
            why: "Formatting money is a presentation decision and parsing it is a trust decision, and the two "
                + "pull in opposite directions. Display wants the reader's culture - the symbol they expect, "
                + "the separators they read fluently - while storage and interchange want a form that means the "
                + "same thing everywhere, because a decimal comma read as a thousands separator changes an "
                + "amount by a factor of a thousand silently. Keeping both surfaces explicit means a developer "
                + "chooses per call site rather than inheriting whatever the machine's culture happens to be, "
                + "which is how the same code produces different numbers in different deployments.",
            expect: "One amount renders seven ways while meaning the same thing - the specifier is a "
                + "presentation choice, not a change of value. The round-trip form re-parses to an equal amount, "
                + "which is what makes it safe for storage; the lenient policy accepts padded and lower-case "
                + "input that strict ISO would not; and malformed text is refused rather than interpreted into a "
                + "plausible wrong number.");

        // Formatting is culture-explicit here so the sample prints the same on every machine.
        var enUS = new CultureInfo("en-US");
        Money money = Money.From(1234.56m, CurrencyCode.USD);

        // The specifier vocabulary is shared by Money and Money<TCurrency> (case-insensitive).
        Console.WriteLine($"  G  (default)     : {money.ToString("G", enUS)}");   // ISO + grouped number
        Console.WriteLine($"  C  (currency)    : {money.ToString("C", enUS)}");   // culture-native symbol
        Console.WriteLine($"  L  (long name)   : {money.ToString("L", enUS)}");   // English currency name
        Console.WriteLine($"  N  (number only) : {money.ToString("N", enUS)}");   // no currency designator
        Console.WriteLine($"  C0 (0 decimals)  : {money.ToString("C0", enUS)}");  // precision override
        Console.WriteLine($"  ~C (elide match) : {money.ToString("~C", enUS)}");  // symbol elided when culture matches

        // MoneyFormatterBuilder captures a reusable display policy once instead of scattering
        // format strings through the code base.
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithEnglishName()
            .WithCulture(enUS)
            .Build();
        Console.WriteLine($"  Formatter        : {formatter.Format(money)}");

        // "R" is the invariant round-trip form: culture-independent, no grouping, natural precision.
        // Pair it with RoundTripOnly parsing for storage and wire formats.
        var wire = money.ToString("R");
        Money restored = Money.Parse(wire, new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly });
        Console.WriteLine($"  R round-trip     : \"{wire}\" -> {restored} (equal: {restored == money})");

        // StrictIso (the default) accepts only canonical "<ISO> <amount>" / "<amount> <ISO>" forms;
        // LenientImport additionally trims white space and upcases the ISO code - for ingesting
        // spreadsheets and external feeds.
        Money<USD> typed = Money<USD>.Parse("USD 99.95", enUS);
        Money imported = Money.Parse("  1234.56 usd ", new MoneyParseOptions { Mode = MoneyParseMode.LenientImport });
        Console.WriteLine($"  StrictIso        : \"USD 99.95\" -> {typed}");
        Console.WriteLine($"  LenientImport    : \"  1234.56 usd \" -> {imported}");

        // TryParse reports malformed input instead of throwing.
        if (!Money.TryParse("not money", enUS, out _))
            Console.WriteLine("  TryParse         : \"not money\" -> false");

        Console.WriteLine();
    }
}
