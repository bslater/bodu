// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChainedAndDefaulting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates the two dictionary decorators: <see cref="LayeredDictionary{TKey, TValue}" />, a live first-wins view
/// over several dictionaries (the .NET analogue of Python's <c>collections.ChainMap</c>), and
/// <see cref="DefaultingDictionary{TKey, TValue}" />, whose indexer materializes missing entries on demand (the
/// analogue of <c>collections.defaultdict</c>).
/// </summary>
/// <remarks>
/// Both decorators make the indexer do more than look up, and both do it in a way worth knowing before you rely on
/// it. A layered write always lands in the <em>first</em> layer, never in the one the value was read from; and a
/// defaulting read is a mutation, so <c>ContainsKey</c> and the indexer disagree on purpose.
/// </remarks>
public static class ChainedAndDefaulting
{
    /// <summary>
    /// Runs the layered-precedence and default-materializing walkthroughs.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "LayeredDictionary / DefaultingDictionary",
            what: "Stacks command-line over file over built-in settings and reads, edits and removes through the " +
                  "merged view, then uses a defaulting dictionary to group and to count without any " +
                  "TryGetValue-or-add boilerplate.",
            why: "Layering is how configuration precedence is actually specified - first layer wins, later layers " +
                 "are fallbacks - and doing it by copying dictionaries together loses the live link to the " +
                 "sources. The two traps are worth stating outright: a write through the view goes to the first " +
                 "layer, shadowing rather than editing whichever layer answered the read, and Remove only deletes " +
                 "from that first layer, so removing a shadow can reveal a value rather than erase it. A " +
                 "defaulting dictionary trades the opposite way: its indexer materializes and stores on a miss, " +
                 "which is what collapses the count-or-create dance into one line - and is also why a casual read " +
                 "grows the dictionary.",
            expect: "log-level comes from the command line, port from the file, host from the built-in layer, and " +
                    "editing the file layer is visible immediately through the view. Removing log-level unshadows " +
                    "the file value rather than deleting the key. ContainsKey and TryGetValue leave Count alone; " +
                    "the indexer raises it.");

        RunLayered();
        RunDefaulting();

        Console.WriteLine();
    }

    /// <summary>
    /// Resolves settings through a command-line / file / built-in defaults chain, then shows shadowing and writes.
    /// </summary>
    private static void RunLayered()
    {
        Console.WriteLine("  LayeredDictionary (first layer wins):");

        // The classic configuration cascade: three sources, highest precedence first. Nothing is copied - the view
        // holds each layer by reference, so this is a resolver, not a merge.
        var commandLine = new Dictionary<string, string>(StringComparer.Ordinal) { ["log-level"] = "trace" };
        var configFile = new Dictionary<string, string>(StringComparer.Ordinal) { ["log-level"] = "info", ["port"] = "8080" };
        var builtIn = new Dictionary<string, string>(StringComparer.Ordinal) { ["log-level"] = "warn", ["port"] = "80", ["host"] = "localhost" };

        var settings = new LayeredDictionary<string, string>(commandLine, configFile, builtIn);

        Console.WriteLine($"    layers       : {settings.Layers.Count}  (expected 3 - searched in order, so position is precedence)");

        // Lookups search the layers in order and the first hit wins, so an earlier layer *shadows* later ones.
        // Count, Keys, and enumeration present the merged view: distinct keys with first-wins values.
        Console.WriteLine($"    log-level    : {settings["log-level"]}  (expected debug - present in all three layers, and the first one wins)");
        Console.WriteLine($"    port         : {settings["port"]}  (the command line does not set it, so the file layer answers)");
        Console.WriteLine($"    host         : {settings["host"]}  (only the last layer has it - the fallback doing its job)");
        Console.WriteLine($"    merged Count : {settings.Count} distinct keys across {settings.Layers.Count} layers  (shadowed duplicates are counted once, not per layer)");

        // Count walks every layer tracking seen keys, so it is O(total entries) rather than a cached O(1) property -
        // worth knowing before calling it in a loop.

        // The layers stay live. Mutating an underlying dictionary directly is visible through the view at once.
        configFile["port"] = "9090";
        Console.WriteLine($"    port after editing the file layer: {settings["port"]}  (the view is live - editing a source is visible at once, with nothing to re-merge)");

        // Writes *through* the view land in the first layer only - ChainMap semantics exactly. This one does not
        // touch the file or built-in layer.
        settings["port"] = "3000";
        Console.WriteLine($"    port after writing through the view: {settings["port"]}  (the write landed in the FIRST layer, shadowing the file rather than editing it)");
        Console.WriteLine($"      commandLine: [{Render(commandLine)}]");
        Console.WriteLine($"      configFile : [{Render(configFile)}]  (untouched - which is the point: a write must not silently rewrite a lower layer)");

        // Remove only reaches the first layer, so a key that exists only deeper cannot be removed - and removing a
        // first-layer entry *unshadows* the value beneath it.
        Console.WriteLine($"    Remove(\"host\")     : {settings.Remove("host")}  (expected False - Remove only touches the first layer, and host is not in it)");
        Console.WriteLine($"    Remove(\"log-level\"): {settings.Remove("log-level")} then log-level = {settings["log-level"]}  (removing the shadow REVEALS the next layer - the key is not gone)");
    }

    /// <summary>
    /// Groups words by their first letter, relying on the indexer to create each missing bucket.
    /// </summary>
    private static void RunDefaulting()
    {
        Console.WriteLine("  DefaultingDictionary (the indexer materializes misses):");

        // The factory is fixed at construction, so the default-producing policy belongs to the dictionary rather
        // than to each call site. This removes the TryGetValue/add dance from the grouping loop below.
        var groups = new DefaultingDictionary<char, List<string>>(_ => []);

        foreach (var word in new[] { "apple", "avocado", "banana", "blueberry", "cherry", "apricot" })
            groups[word[0]].Add(word); // one indexer read both creates and returns the bucket

        foreach (var key in groups.Keys.OrderBy(key => key))
            Console.WriteLine($"    {key}: [{string.Join(", ", groups[key])}]");

        // Only the indexer getter invokes the factory - exactly as Python's __missing__ fires only for d[key].
        // TryGetValue, ContainsKey, Count, and enumeration observe stored entries only, so a probe never mutates.
        var counters = new DefaultingDictionary<string, int>(_ => 0);
        Console.WriteLine($"    ContainsKey(\"z\") : {counters.ContainsKey("z")}, Count {counters.Count}  (expected False and unchanged - the probe answers honestly and creates nothing)");
        Console.WriteLine($"    TryGetValue(\"z\") : {counters.TryGetValue("z", out _)}, Count {counters.Count}  (also non-materializing - use these two when you must not grow the dictionary)");

        // A plain indexer read is what stores the default; the entry then exists for good.
        Console.WriteLine($"    counters[\"z\"]    : {counters["z"]}, Count {counters.Count}  (the indexer materialized the default AND stored it, so Count rose on a read)");

        // Because the miss is stored rather than recomputed, compound assignment works on a first sighting.
        counters["z"] += 5;
        Console.WriteLine($"    counters[\"z\"] += 5 -> {counters["z"]}  (the whole point: compound assignment on a missing key just works, no TryGetValue-or-add)");

        // The factory receives the key, so defaults can depend on it.
        var widths = new DefaultingDictionary<string, int>(key => key.Length);
        Console.WriteLine($"    widths[\"alpha\"]  : {widths["alpha"]}  (the factory receives the key, so a default can be computed from it rather than being a constant)");
    }

    /// <summary>
    /// Renders a dictionary's entries in key order.
    /// </summary>
    /// <param name="layer">The dictionary to render.</param>
    /// <returns>A comma-separated list of key/value pairs.</returns>
    private static string Render(Dictionary<string, string> layer) =>
        string.Join(", ", layer.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"));
}
