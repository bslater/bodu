// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Samples.Text.Configuration.ConfigCascade.Scenarios;

namespace Bodu.Samples.Text.Configuration.ConfigCascade;

/// <summary>
/// Entry point for the configuration-cascade sample: the <c>Bodu.Text.Configuration</c>
/// pipeline — parse (with diagnostics), path-targeted resolution where matching sections
/// cascade EditorConfig-style, typed view getters with <c>unset</c> handling, and saving a
/// mutated document with comments preserved. Everything runs offline against the committed
/// <c>Data/sample.boduconfig</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Configuration.Samples.ConfigCascade");
        Console.WriteLine("=============================================");
        Console.WriteLine();

        ParseAndDiagnostics.Run();
        ResolveCascade.Run();
        UnsetAndPresets.Run();
        SaveRoundTrip.Run();

        Console.WriteLine("Done.");
    }
}
