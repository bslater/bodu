// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Samples.Extensions.Configuration.Text.BridgeHosting.Scenarios;

namespace Bodu.Samples.Extensions.Configuration.Text.BridgeHosting;

/// <summary>
/// Entry point for the configuration-bridge sample: flowing Bodu text formats into the standard
/// <c>Microsoft.Extensions.Configuration</c> pipeline — a <c>.boduconfig</c> cascade resolved for
/// a target path, a TOML file flattened to configuration keys, and strongly typed
/// <c>IOptions&lt;T&gt;</c> binding through dependency injection. Everything runs offline against
/// the committed <c>Data/</c> files.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Extensions.Configuration.Text.Samples.BridgeHosting");
        Console.WriteLine("=========================================================");
        Console.WriteLine();

        TextConfigurationFileSource.Run();
        TomlFileSource.Run();
        OptionsBinding.Run();

        Console.WriteLine("Done.");
    }
}
