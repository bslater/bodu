// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Functional;

namespace Bodu.Core.Samples.FunctionalRailway.Scenarios;

/// <summary>
/// Demonstrates <see cref="Option{T}" /> as a total replacement for a nullable return: a lookup
/// either yields a value (<c>Some</c>) or explicitly none (<c>None</c>), and callers transform the
/// value with <c>Map</c>/<c>Bind</c>/<c>Filter</c> without ever branching on <see langword="null" />.
/// </summary>
/// <remarks>
/// The gain over a nullable return is that absence becomes part of the type rather than a convention. A method
/// returning <c>int?</c> relies on every caller remembering to check; one returning <c>Option&lt;int&gt;</c> cannot
/// have its value read without the absent case being handled. The operators then let a whole chain be written once
/// for the present case, with absence carried through untouched.
/// </remarks>
public static class OptionBasics
{
    private static readonly Dictionary<string, int> Inventory = new()
    {
        ["widget"] = 12,
        ["gadget"] = 0,
        ["sprocket"] = 7,
    };

    /// <summary>
    /// Looks items up as options and composes transformations over the present cases only.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Option<T> - present-or-absent without null",
            what: "Looks up a present and a missing key, then chains Map, Filter and GetValueOrDefault over both " +
                  "to show the operators running only when there is a value.",
            why: "A nullable return puts the burden on the caller to remember a check, and forgetting is the most " +
                 "common bug there is. Option moves absence into the type, so the value cannot be reached without " +
                 "the None case being dealt with. The operators are the other half: Map and Filter apply only to " +
                 "Some and pass None straight through, so a four-step transformation needs one null check at the " +
                 "end rather than four along the way.",
            expect: "widget resolves to Some(12) and missing to None. The chained calls never run against an " +
                    "absent value, so the missing lookups return the supplied fallback rather than throwing, and " +
                    "a Filter that rejects its input turns a Some into a None.");

        // A miss is a first-class None, not a null or a sentinel.
        Console.WriteLine($"  widget   : {Describe(Lookup("widget"))}  (expected Some(12) - present, and the value is only reachable through the Some case)");
        Console.WriteLine($"  missing  : {Describe(Lookup("missing"))}  (expected None - absence is a value here, not a null to be checked for later)");

        // Map transforms the value when present; None flows straight through untouched.
        Option<string> label = Lookup("sprocket").Map(qty => $"{qty} in stock");
        Console.WriteLine($"  sprocket : {label.GetValueOrDefault("(none)")}  (Map ran because the lookup was Some; GetValueOrDefault is where the chain finally leaves Option)");

        // Filter demotes a Some to None when the predicate fails - here "in stock" means qty > 0.
        Option<int> inStock = Lookup("gadget").Filter(qty => qty > 0);
        Console.WriteLine($"  gadget   : in stock? {inStock.IsSome}  (expected False - Filter rejected the value, turning a Some into a None without any branch being written)");

        // Match collapses both cases to a single result value.
        var summary = Lookup("widget").Match(
            onSome: qty => $"reorder {(qty < 20 ? "yes" : "no")}",
            onNone: () => "unknown item");
        Console.WriteLine($"  widget   : {summary}  (the whole chain expressed once for the present case; an absent lookup would have skipped every step)");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns the stock level for a key as an option, converting a dictionary miss into <c>None</c>.
    /// </summary>
    /// <param name="key">The item key to look up.</param>
    /// <returns>The stock level wrapped in <see cref="Option{T}" />, or <c>None</c> when absent.</returns>
    private static Option<int> Lookup(string key) =>
        Inventory.TryGetValue(key, out var qty) ? Option.Some(qty) : Option.None<int>();

    /// <summary>
    /// Renders an option as a short human-readable string.
    /// </summary>
    /// <param name="option">The option to describe.</param>
    /// <returns>A description of the option's state.</returns>
    private static string Describe(Option<int> option) =>
        option.Match(qty => $"Some({qty})", () => "None");
}
