// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VerifyManifest.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

namespace Bodu.IO.Hashing.Samples.FileIntegrity.Scenarios;

/// <summary>
/// Checks the release folder against its manifest after both have been damaged, reporting each entry as
/// <c>OK</c>, <c>BAD</c>, <c>MISSING</c>, or <c>INVALID</c>. <c>TryVerifyHash(byte[], byte[], out bool)</c> separates
/// an entry that could not be checked from one that failed, and <c>VerifyHash(Stream, string)</c> /
/// <c>VerifyHashAsync(Stream, string)</c> check a large file without loading it.
/// </summary>
public static class VerifyManifest
{
    /// <summary>
    /// Damages the folder and the manifest, then checks every entry.
    /// </summary>
    /// <param name="folder">The release folder the manifest describes.</param>
    /// <param name="manifest">The manifest built for the undamaged folder.</param>
    /// <returns>A task that completes when the scenario has printed its results.</returns>
    public static async Task RunAsync(ReleaseFolder folder, IReadOnlyList<string> manifest)
    {
        SampleConsole.Scenario(
            "Checking a folder against its manifest",
            what: "Flips one bit in app.dat, appends a manifest line for a file that was never shipped and a line " +
                  "whose digest is not hexadecimal, then checks every entry with TryVerifyHash(byte[], byte[], out " +
                  "bool). Finally re-checks the two binary files straight from their streams with VerifyHash(Stream, " +
                  "string) and VerifyHashAsync(Stream, string).",
            why: "A checker has two different ways to fail and they need different responses: the file does not " +
                 "match (BAD: re-download it), or the entry could not be checked at all (MISSING or INVALID: fix " +
                 "the manifest or the folder). VerifyHash folds both into false. The out-bool overload of " +
                 "TryVerifyHash returns false only when it could not compare, and reports the comparison itself in " +
                 "its out argument, so the checker can tell them apart without exceptions.",
            expect: "readme.txt and assets.pak print OK, app.dat prints BAD because one bit changed, license.txt " +
                    "prints MISSING and the garbled line prints INVALID. The stream checks agree: app.dat False and " +
                    "assets.pak True, synchronously and asynchronously.");

        // One flipped bit is enough for a CRC to notice; this is the accidental-corruption case it is built for.
        var appPath = folder.GetPath("app.dat");
        var app = await File.ReadAllBytesAsync(appPath).ConfigureAwait(false);
        app[50_000] ^= 0x04;
        await File.WriteAllBytesAsync(appPath, app).ConfigureAwait(false);

        // A manifest damaged as hand-edited manifests are: a file that was never shipped, and a garbled digest.
        var damaged = new List<string>(manifest)
        {
            "0BADC0DE  license.txt",
            "not-a-crc  readme.txt",
        };

        var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
        foreach (var line in damaged)
        {
            var separator = line.IndexOf("  ", StringComparison.Ordinal);
            var expectedHex = line[..separator];
            var name = line[(separator + 2)..];
            var path = folder.GetPath(name);

            // Absent inputs are passed through as null on purpose: this overload treats a null input or expected
            // digest as "could not verify" and returns false, rather than throwing.
            byte[]? content = File.Exists(path) ? await File.ReadAllBytesAsync(path).ConfigureAwait(false) : null;
            var expected = Hex.TryDecode(expectedHex);

            var status = crc.TryVerifyHash(content!, expected!, out var matches)
                ? (matches ? "OK" : "BAD")
                : content is null ? "MISSING" : "INVALID";

            Console.WriteLine($"  {status,-8} {name,-12} (manifest: {expectedHex})");
        }

        Console.WriteLine();

        // A large file need not be loaded to be checked: the stream forms read it through a pooled buffer.
        var appHex = ExpectedHex(manifest, "app.dat");
        var assetsHex = ExpectedHex(manifest, "assets.pak");

        bool appOk;
        using (var stream = File.OpenRead(appPath))
            appOk = crc.VerifyHash(stream, appHex);

        bool assetsOk;
        using (var stream = File.OpenRead(folder.GetPath("assets.pak")))
            assetsOk = await crc.VerifyHashAsync(stream, assetsHex).ConfigureAwait(false);

        bool appOkAsync;
        using (var stream = File.OpenRead(appPath))
            appOkAsync = await crc.VerifyHashAsync(stream, appHex).ConfigureAwait(false);

        Console.WriteLine($"  VerifyHash(Stream, string), app.dat         : {appOk}");
        Console.WriteLine($"  VerifyHashAsync(Stream, string), app.dat    : {appOkAsync}");
        Console.WriteLine($"  VerifyHashAsync(Stream, string), assets.pak : {assetsOk}");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns the digest text the manifest records for a file.
    /// </summary>
    /// <param name="manifest">The manifest lines.</param>
    /// <param name="name">The file name to look up.</param>
    /// <returns>The hexadecimal digest text.</returns>
    private static string ExpectedHex(IReadOnlyList<string> manifest, string name) =>
        manifest.Single(line => line.EndsWith("  " + name, StringComparison.Ordinal))[..8];
}
