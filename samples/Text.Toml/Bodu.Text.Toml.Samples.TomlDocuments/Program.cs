// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Samples.TomlDocuments.Scenarios;

namespace Bodu.Text.Toml.Samples.TomlDocuments;

/// <summary>
/// Entry point for the TOML document-model sample: the layers beneath <c>TomlSerializer</c> —
/// the mutable <c>TomlNode</c> DOM, the read-only <c>TomlDocument</c> DOM, and the ref-struct
/// <c>Utf8TomlReader</c>/<c>Utf8TomlWriter</c> token surface, including streaming reads across
/// buffer boundaries. Everything runs offline against the committed <c>Data/server-config.toml</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Toml.Samples.TomlDocuments");
        Console.WriteLine("====================================");
        Console.WriteLine();

        MutableDom.Run();
        ReadOnlyDom.Run();
        TokenReaderWriter.Run();
        StreamingReads.Run();

        Console.WriteLine("Done.");
    }
}
