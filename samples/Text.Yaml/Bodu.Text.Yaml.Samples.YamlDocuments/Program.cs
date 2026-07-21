// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Samples.YamlDocuments.Scenarios;

namespace Bodu.Text.Yaml.Samples.YamlDocuments;

/// <summary>
/// Entry point for the YAML document-model sample: the layers beneath <c>YamlSerializer</c> — the
/// ref-struct <c>Utf8YamlWriter</c>/<c>Utf8YamlReader</c> token surface, the mutable <c>YamlNode</c>
/// DOM, the read-only <c>YamlDocument</c> DOM, and the stream/async serializer facade. Everything
/// runs offline against the committed <c>Data/server-config.yaml</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    /// <returns>A task that completes when every scenario has run.</returns>
    public static async Task Main()
    {
        Console.WriteLine("Bodu.Text.Yaml.Samples.YamlDocuments");
        Console.WriteLine("====================================");
        Console.WriteLine();

        TokenReaderWriter.Run();
        MutableDom.Run();
        ReadOnlyDom.Run();
        await StreamingReads.RunAsync();

        Console.WriteLine("Done.");
    }
}
