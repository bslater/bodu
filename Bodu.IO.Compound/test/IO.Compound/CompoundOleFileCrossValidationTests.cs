// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundOleFileCrossValidationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Security.Cryptography;
using Bodu.IO.Compound.Builders;
using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Cross-validates builder output against the independent python <c>olefile</c> parser, proving the produced containers
/// are conformant beyond this library's own reader.
/// </summary>
/// <remarks>
/// These tests require <c>python3</c> with the <c>olefile</c> package. When it is unavailable the tests report an
/// inconclusive result rather than failing, so the suite remains runnable offline and without python.
/// </remarks>
[TestClass]
public class CompoundOleFileCrossValidationTests
{
    /// <summary>
    /// Verifies that python <c>olefile</c> reads a written version-3 container with the same streams and content.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Save_WhenReadByOleFile_ForVersion3_ShouldMatchStreamHashes() =>
        AssertCrossValidates(CompoundFileVersion.V3);

    /// <summary>
    /// Verifies that python <c>olefile</c> reads a written version-4 container with the same streams and content.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Save_WhenReadByOleFile_ForVersion4_ShouldMatchStreamHashes() =>
        AssertCrossValidates(CompoundFileVersion.V4);

    /// <summary>
    /// Builds a representative container, writes it, and asserts that <c>olefile</c> reports the same stream hashes.
    /// </summary>
    /// <param name="version">The compound-file version to write.</param>
    private static void AssertCrossValidates(CompoundFileVersion version)
    {
        if (!IsOleFileAvailable())
            Assert.Inconclusive("python3 with the olefile package is not available.");

        Dictionary<string, byte[]> streams = new(StringComparer.Ordinal)
        {
            ["Small"] = [1, 2, 3, 4, 5],
            ["Big"] = CreatePayload(20000),
            ["Storage 1/Nested"] = [9, 9, 9],
        };

        var builder = new CompoundFileBuilder(new CompoundBuildOptions { Version = version });
        _ = builder.Root.AddStream("Small", streams["Small"]);
        _ = builder.Root.AddStream("Big", streams["Big"]);
        _ = builder.Root.AddStorage("Storage 1").AddStream("Nested", streams["Storage 1/Nested"]);

        string path = Path.Combine(Path.GetTempPath(), $"bodu-cfb-{Guid.NewGuid():N}.cfb");
        try
        {
            builder.Save(path);
            Dictionary<string, string> reported = RunOleFile(path);

            foreach (KeyValuePair<string, byte[]> expected in streams)
            {
                Assert.IsTrue(reported.TryGetValue(expected.Key, out string? hash), $"olefile missing {expected.Key}");
                Assert.AreEqual(Convert.ToHexString(SHA256.HashData(expected.Value)).ToLowerInvariant(), hash, expected.Key);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Determines whether python and the olefile package are available.
    /// </summary>
    /// <returns><see langword="true" /> when both are present; otherwise <see langword="false" />.</returns>
    private static bool IsOleFileAvailable()
    {
        try
        {
            using Process? probe = Process.Start(new ProcessStartInfo("python3", "-c \"import olefile\"")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });

            if (probe is null)
                return false;

            probe.WaitForExit(20000);
            return probe.HasExited && probe.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Runs olefile over a compound file and returns each stream's lowercase hexadecimal SHA-256 keyed by path.
    /// </summary>
    /// <param name="path">The compound-file path.</param>
    /// <returns>A map from storage-qualified stream path to hash.</returns>
    private static Dictionary<string, string> RunOleFile(string path)
    {
        const string script =
            "import olefile,sys,hashlib\n" +
            "ole=olefile.OleFileIO(sys.argv[1])\n" +
            "for p in ole.listdir(streams=True,storages=False):\n" +
            "  d=ole.openstream(p).read()\n" +
            "  sys.stdout.write('/'.join(p)+'\\t'+hashlib.sha256(d).hexdigest()+'\\n')\n" +
            "ole.close()\n";

        using Process process = Process.Start(new ProcessStartInfo("python3")
        {
            ArgumentList = { "-c", script, path },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(60000);

        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split('\t');
            if (parts.Length == 2)
                map[parts[0]] = parts[1].Trim();
        }

        return map;
    }

    /// <summary>
    /// Creates a deterministic payload of the requested length.
    /// </summary>
    /// <param name="size">The payload length.</param>
    /// <returns>The generated payload.</returns>
    private static byte[] CreatePayload(int size)
    {
        byte[] payload = new byte[size];
        for (int i = 0; i < size; i++)
            payload[i] = (byte)((i * 31) + 7);

        return payload;
    }
}
