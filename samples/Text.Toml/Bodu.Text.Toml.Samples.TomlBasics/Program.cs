// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

namespace Bodu.Text.Toml.Samples.TomlBasics;

/// <summary>
/// Entry point for the TOML-basics sample: the <c>TomlSerializer</c> POCO surface — round trips,
/// the four native temporal kinds, naming policies and attributes, and the spec-version and
/// byte-array knobs. Everything runs offline against the committed <c>Data/app-config.toml</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Toml.Samples.TomlBasics");
        Console.WriteLine("=================================");
        Console.WriteLine();

        SerializerRoundTrip.Run();
        TemporalKinds.Run();
        NamingAndAttributes.Run();
        SpecVersionAndBytes.Run();

        Console.WriteLine("Done.");
    }
}
