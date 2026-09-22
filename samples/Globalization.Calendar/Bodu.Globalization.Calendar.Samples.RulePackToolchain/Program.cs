// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.RulePackToolchain.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.RulePackToolchain;

/// <summary>
/// Entry point for the rule-pack toolchain sample: <c>Bodu.Globalization.Calendar.Build</c> and the
/// <c>bodu-calendar</c> tool from <c>Bodu.Globalization.Calendar.Tool</c>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike every other sample, the interesting part of this one happens at <b>build</b> time. The csproj declares
/// <c>rules/company-holidays.xml</c> as a <c>NotableDatePack</c> item; the MSBuild task lints and compiles it with
/// the tool and copies the sealed <c>.bcal</c> pack beside this application. What runs here is only the consumer
/// side: loading that pack.
/// </para>
/// <para>
/// Nothing here reads the network, and the compile step is incremental — a rebuild with no edit to the document
/// does no work.
/// </para>
/// </remarks>
public static class Program
{
    /// <summary>
    /// Locates the compiled pack, then runs the scenario over it.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.RulePackToolchain");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        // The .targets copies each compiled pack beside the application, named after its source document.
        var packPath = Path.Combine(AppContext.BaseDirectory, "company-holidays.bcal");
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "rules", "company-holidays.xml");

        if (!File.Exists(packPath))
        {
            Console.WriteLine($"Compiled pack not found at {packPath}.");
            Console.WriteLine("The pack is produced by the build, so build this project (or the solution) first.");
            return;
        }

        Console.WriteLine("The build compiled rules/company-holidays.xml to company-holidays.bcal via the");
        Console.WriteLine("CompileNotableDatePack MSBuild task, which invoked the bodu-calendar tool. That step");
        Console.WriteLine("is the sample; what follows is a consumer reading its output.");
        Console.WriteLine();

        LoadingACompiledPack.Run(packPath, xmlPath);

        Console.WriteLine("Done.");
    }
}
