// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteCatalogTests.Checkout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteCatalogTests
{
    /// <summary>
    /// The git pathspecs that select every release-note catalog under <c>corpus/</c> and every copy a test project
    /// embeds.
    /// </summary>
    private static readonly string[] s_catalogPathspecs =
        ["corpus/*/fixes/*-fixes.csv", "*/test/Fixtures/ReleaseNotes/*-fixes.csv"];

    /// <summary>
    /// Verifies that every release-note catalog in the repository loads with no problems as git checks it out where
    /// line endings are converted (<c>core.autocrlf=true</c>, the default of Git for Windows), so that a catalog
    /// keeps the line feeds <see cref="ReleaseNoteCatalog.Load(string, ReadOnlySpan{byte})" /> requires on every
    /// platform.
    /// </summary>
    /// <returns>A task that completes when the catalogs have been checked.</returns>
    [TestMethod]
    public async Task Load_WhenRepositoryCatalogIsCheckedOutWithLineEndingConversion_ShouldReportNoProblems()
    {
        string? root = CorpusTree.FindRepositoryRoot();
        if (root is null)
            Assert.Inconclusive("The repository root, the directory that holds bodu.slnx, was not found.");

        GitResult listing = await RunGitAsync(root, ["ls-files", "-z", "--", .. s_catalogPathspecs]);
        if (!listing.Succeeded)
            Assert.Inconclusive("git could not list the repository's files: " + listing.Error);

        string[] paths = Encoding.UTF8.GetString(listing.Output).Split('\0', StringSplitOptions.RemoveEmptyEntries);
        Assert.IsTrue(paths.Length > 0, "git listed no release-note catalogues.");

        var failures = new List<string>();
        foreach (string path in paths)
        {
            // What git writes to the working tree for the indexed file, under the repository's attributes, where
            // automatic line-ending conversion is on.
            GitResult checkout = await RunGitAsync(root, ["-c", "core.autocrlf=true", "cat-file", "--filters", ":" + path]);
            Assert.IsTrue(checkout.Succeeded, $"git could not read {path}: {checkout.Error}");

            ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load(Path.GetFileName(path), checkout.Output);
            if (catalog.Problems.Count > 0)
                failures.Add($"{path}: {catalog.Problems.Count} problem(s), the first: {catalog.Problems[0]}");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Runs git in a directory and collects what it writes.
    /// </summary>
    /// <param name="workingDirectory">The directory git runs in.</param>
    /// <param name="arguments">The arguments passed to git.</param>
    /// <returns>
    /// The bytes git wrote to its standard output and the text it wrote to its standard error, with whether it could be
    /// started and exited with code zero.
    /// </returns>
    private static async Task<GitResult> RunGitAsync(string workingDirectory, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Win32Exception exception)
        {
            return new GitResult(false, [], exception.Message);
        }

        if (process is null)
            return new GitResult(false, [], "the git process did not start.");

        using (process)
        {
            Task<string> error = process.StandardError.ReadToEndAsync();
            using var output = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(output);
            await process.WaitForExitAsync();

            return new GitResult(process.ExitCode == 0, output.ToArray(), (await error).Trim());
        }
    }

    /// <summary>
    /// Holds the outcome of one git command.
    /// </summary>
    /// <param name="Succeeded">Whether git started and exited with code zero.</param>
    /// <param name="Output">The bytes git wrote to its standard output.</param>
    /// <param name="Error">The text git wrote to its standard error, or why it could not be started.</param>
    private sealed record GitResult(bool Succeeded, byte[] Output, string Error);
}
