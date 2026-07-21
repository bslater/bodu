// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

namespace Bodu.Text.Yaml.Samples.YamlBasics;

/// <summary>
/// Entry point for the YAML-basics sample: the <c>YamlSerializer</c> POCO surface — round trips,
/// YAML's implicit scalar typing, sequences and mappings binding to collections, naming policies
/// and attributes, and the spec-version, style, and duplicate/merge-key knobs. Everything runs
/// offline against the committed <c>Data/app-config.yaml</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Yaml.Samples.YamlBasics");
        Console.WriteLine("=================================");
        Console.WriteLine();

        SerializerRoundTrip.Run();
        ScalarKinds.Run();
        CollectionsAndDictionaries.Run();
        NamingAndAttributes.Run();
        SpecAndStyles.Run();

        Console.WriteLine("Done.");
    }
}
