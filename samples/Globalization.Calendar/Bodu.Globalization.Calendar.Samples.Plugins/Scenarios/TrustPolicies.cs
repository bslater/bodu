// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrustPolicies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Globalization.Calendar.Plugins;

namespace Bodu.Globalization.Calendar.Samples.Plugins.Scenarios;

/// <summary>
/// Demonstrates the trust gate: loading a plugin runs arbitrary code, so the loader requires an
/// <see cref="IPluginTrustPolicy" /> and refuses anything the policy does not vouch for.
/// </summary>
public static class TrustPolicies
{
    /// <summary>
    /// Loads the plugin under a hash allow-list that matches, then under one that does not, then shows a composed
    /// policy.
    /// </summary>
    /// <param name="pluginPath">The plugin assembly to load.</param>
    public static void Run(string pluginPath)
    {
        SampleConsole.Scenario(
            "The trust gate - why loading a plugin takes a policy",
            what: "Pins the plugin with a SHA-256 allow-list and loads it, then tries the same load with a "
                + "deliberately wrong hash, then shows a delegating policy and a composite that requires several "
                + "policies to agree.",
            why: "Loading a plugin executes whatever is in that file, in-process, with the host's privileges. That "
                + "is why the loader has no parameterless overload: a policy is not optional, so the decision "
                + "cannot be skipped by accident, only made explicitly. A hash allow-list is the strongest simple "
                + "answer - the operator records the exact bytes they approved, and any modification, including one "
                + "that keeps the file name and version, stops matching. AllowAllPluginTrustPolicy exists for "
                + "assemblies you built yourself, and is the wrong answer for anything a user can drop into a "
                + "directory.",
            expect: "The matching hash loads and reports the plugin name. The wrong hash raises "
                + "PluginNotTrustedException carrying the reason, and nothing from the assembly runs - refusal "
                + "happens before any plugin code executes. The composite policy refuses because one of its two "
                + "members refuses, which is the point of composing them.");

        // The hash an operator would have recorded when they approved this exact file.
        byte[] actualHash;
        using (var stream = File.OpenRead(pluginPath))
            actualHash = SHA256.HashData(stream);

        var assemblyName = Path.GetFileNameWithoutExtension(pluginPath);
        Console.WriteLine($"  Assembly                : {assemblyName}");
        Console.WriteLine($"  SHA-256                 : {Convert.ToHexString(actualHash)[..32]}...   (the full digest is what an operator pins)");
        Console.WriteLine();

        // 1. The allow-list carries the real hash: the load is permitted.
        var matching = new FileHashPluginTrustPolicy(
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { [assemblyName] = actualHash });

        using (NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile(pluginPath, matching))
        {
            Console.WriteLine($"  matching hash           : loaded '{handle.Plugin.Name}'   (the bytes on disk are the bytes that were approved)");
        }

        // 2. The allow-list carries a different hash: the load is refused before any plugin code runs.
        var wrongHash = new byte[32];
        wrongHash[0] = 0xFF;
        var mismatched = new FileHashPluginTrustPolicy(
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { [assemblyName] = wrongHash });

        try
        {
            using var refused = NotableDatePluginLoader.LoadFromFile(pluginPath, mismatched);
            Console.WriteLine("  mismatched hash         : LOADED - this line should never print");
        }
        catch (PluginNotTrustedException ex)
        {
            Console.WriteLine($"  mismatched hash         : refused - {ex.Message}");
        }

        Console.WriteLine();

        // 3. A delegating policy is the seam for a host's own rule - a signing check, an
        //    allow-list from configuration, a directory the operator controls.
        var byName = new DelegatingPluginTrustPolicy(context =>
            context.AssemblyName?.StartsWith("Bodu.", StringComparison.Ordinal) == true
                ? new PluginTrustResult(true, "name is in the Bodu namespace")
                : new PluginTrustResult(false, "assembly name is outside the approved namespace"));

        using (NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile(pluginPath, byName))
        {
            Console.WriteLine($"  delegating policy       : loaded '{handle.Plugin.Name}'   (a host's own rule, expressed as a lambda)");
        }

        // 4. Composing policies requires every member to agree, so adding one can only narrow trust.
        var composite = new CompositePluginTrustPolicy(byName, mismatched);

        try
        {
            using var refused = NotableDatePluginLoader.LoadFromFile(pluginPath, composite);
            Console.WriteLine("  composite policy        : LOADED - this line should never print");
        }
        catch (PluginNotTrustedException ex)
        {
            Console.WriteLine($"  composite policy        : refused - {ex.Message}");
            Console.WriteLine("                            (the name policy accepted and the hash policy did not; composition takes the strictest answer)");
        }

        Console.WriteLine();
    }
}
