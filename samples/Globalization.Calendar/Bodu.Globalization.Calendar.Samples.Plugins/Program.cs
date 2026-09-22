// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.Plugins.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.Plugins;

/// <summary>
/// Entry point for the plugin sample: <c>Bodu.Globalization.Calendar.Plugins</c> loading a
/// date-calculation algorithm out of a separate assembly, under a trust policy. The plugin lives in
/// the sibling <c>…Samples.Plugin.Contoso</c> project, which this host references with
/// <c>ReferenceOutputAssembly=false</c> — so the DLL is built and copied beside the host, but its
/// types are unavailable at compile time and the host can only reach it through the loader.
/// Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Locates the built plugin assembly and runs every scenario over it.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.Plugins");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        // The build drops the plugin into a 'plugins' folder beside the host, which is the shape a
        // real deployment uses: a directory an operator adds assemblies to.
        var pluginPath = Path.Combine(
            AppContext.BaseDirectory,
            "plugins",
            "Bodu.Globalization.Calendar.Samples.Plugin.Contoso.dll");

        if (!File.Exists(pluginPath))
        {
            Console.WriteLine($"Plugin assembly not found at {pluginPath}.");
            Console.WriteLine("Build the solution (or this project) first so the plugin is produced and copied.");
            return;
        }

        LoadingAPlugin.Run(pluginPath);
        TrustPolicies.Run(pluginPath);

        Console.WriteLine("Done.");
    }
}
