// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

namespace Bodu.Samples.Text.Formats.ConfigFiles;

/// <summary>
/// Entry point for the config-files sample: the INI and DotEnv formats from <c>Bodu.Text.Ini</c>
/// and <c>Bodu.Text.DotEnv</c> — reading typed values, mutating and re-emitting an INI document
/// with its comments intact, and the deliberately literal (no-interpolation) DotEnv contract.
/// Everything runs offline against the committed <c>Data/app.ini</c> and <c>Data/env.sample</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Formats.Samples.ConfigFiles");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        IniReadTypedValues.Run();
        IniMutateAndFormat.Run();
        DotEnvBasics.Run();
        DotEnvStreamingReader.Run();

        Console.WriteLine("Done.");
    }
}
