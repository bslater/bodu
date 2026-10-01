// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Samples.FileIntegrity.Scenarios;

namespace Bodu.IO.Hashing.Samples.FileIntegrity;

/// <summary>
/// Entry point for the file-integrity sample: the <c>Bodu.IO.Hashing.Extensions</c> stream, async, and verify
/// surface applied to real files - building a checksum manifest for a release folder, checking the folder against it
/// after it has been damaged, and digesting a file delivered as numbered parts without reassembling it. The files are
/// written to a temporary folder with fixed contents and removed afterwards, so all output is deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Creates the release folder, then runs every scenario in order against it.
    /// </summary>
    /// <returns>A task that completes when every scenario has finished.</returns>
    public static async Task Main()
    {
        Console.WriteLine("Bodu.IO.Hashing.Samples.FileIntegrity");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        using (var folder = ReleaseFolder.Create())
        {
            var manifest = await BuildManifest.RunAsync(folder).ConfigureAwait(false);
            await VerifyManifest.RunAsync(folder, manifest).ConfigureAwait(false);
            await MultiPartDigests.RunAsync(folder).ConfigureAwait(false);
        }

        Console.WriteLine("Done.");
    }
}
