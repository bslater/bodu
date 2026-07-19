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
        Console.WriteLine("--- Option<T>: present-or-absent without null ---");

        // A miss is a first-class None, not a null or a sentinel.
        Console.WriteLine($"widget   : {Describe(Lookup("widget"))}");
        Console.WriteLine($"missing  : {Describe(Lookup("missing"))}");

        // Map transforms the value when present; None flows straight through untouched.
        Option<string> label = Lookup("sprocket").Map(qty => $"{qty} in stock");
        Console.WriteLine($"sprocket : {label.GetValueOrDefault("(none)")}");

        // Filter demotes a Some to None when the predicate fails - here "in stock" means qty > 0.
        Option<int> inStock = Lookup("gadget").Filter(qty => qty > 0);
        Console.WriteLine($"gadget   : in stock? {inStock.IsSome}");

        // Match collapses both cases to a single result value.
        var summary = Lookup("widget").Match(
            onSome: qty => $"reorder {(qty < 20 ? "yes" : "no")}",
            onNone: () => "unknown item");
        Console.WriteLine($"widget   : {summary}");

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
