// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Test.Corpus;

/// <summary>
/// Locates the repository's <c>corpus/</c> tree and checks that a test project's embedded release-note catalogues are
/// byte-for-byte copies of their sources there.
/// </summary>
/// <remarks>
/// A catalogue's source of truth is <c>corpus/&lt;area&gt;/fixes/&lt;library&gt;-fixes.csv</c>; the test project embeds
/// a copy under <c>Fixtures/ReleaseNotes/</c> so its tests run without the repository. The check keeps the two from
/// drifting apart. It reports inconclusive, rather than failing, when the tests run outside a checkout.
/// </remarks>
public static class CorpusTree
{
    /// <summary>The solution file that marks the repository root.</summary>
    private const string SolutionFileName = "bodu.slnx";

    /// <summary>The part of an embedded resource name that marks a catalogue copied into a test project.</summary>
    private const string ResourceMarker = ".Fixtures.ReleaseNotes.";

    /// <summary>
    /// Finds the repository root by walking up from a directory to the one that holds the solution file.
    /// </summary>
    /// <param name="startDirectory">
    /// The directory to start from; <see cref="AppContext.BaseDirectory" /> when <see langword="null" />.
    /// </param>
    /// <returns>The repository root, or <see langword="null" /> when no ancestor holds <c>bodu.slnx</c>.</returns>
    public static string? FindRepositoryRoot(string? startDirectory = null)
    {
        for (DirectoryInfo? directory = new(startDirectory ?? AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
                return directory.FullName;
        }

        return null;
    }

    /// <summary>
    /// Asserts that the catalogues an assembly embeds under <c>Fixtures/ReleaseNotes/</c> are exactly the files in
    /// <c>corpus/&lt;area&gt;/fixes/</c>, byte for byte.
    /// </summary>
    /// <param name="assembly">The test assembly that embeds the catalogues.</param>
    /// <param name="area">The corpus area, such as <c>toml</c>.</param>
    /// <param name="repositoryRoot">
    /// The repository root; found with <see cref="FindRepositoryRoot" /> when <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assembly" /> or <paramref name="area" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="AssertInconclusiveException">The repository root cannot be found.</exception>
    /// <exception cref="AssertFailedException">
    /// The area's <c>fixes</c> directory does not exist, or a catalogue is missing from one side or differs between
    /// them.
    /// </exception>
    public static void AssertEmbeddedCopiesMatch(Assembly assembly, string area, string? repositoryRoot = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(area);

        string? root = repositoryRoot ?? FindRepositoryRoot();
        if (root is null)
        {
            Assert.Inconclusive($"The repository root ({SolutionFileName}) was not found from the test base directory.");
            return;
        }

        string directory = Path.Combine(root, "corpus", area, "fixes");
        Assert.IsTrue(Directory.Exists(directory), $"The corpus directory corpus/{area}/fixes does not exist.");

        var embedded = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            int marker = resource.IndexOf(ResourceMarker, StringComparison.Ordinal);
            if (marker < 0 || !resource.EndsWith(ReleaseNoteCatalog.FileNameSuffix, StringComparison.Ordinal))
                continue;

            using Stream stream = assembly.GetManifestResourceStream(resource)!;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            embedded[resource[(marker + ResourceMarker.Length)..]] = copy.ToArray();
        }

        var differences = new List<string>();
        var sources = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(directory, "*" + ReleaseNoteCatalog.FileNameSuffix))
        {
            string fileName = Path.GetFileName(path);
            sources.Add(fileName);

            if (!embedded.TryGetValue(fileName, out byte[]? copy))
                differences.Add($"corpus/{area}/fixes/{fileName} is not embedded under Fixtures/ReleaseNotes/");
            else if (!copy.AsSpan().SequenceEqual(File.ReadAllBytes(path)))
                differences.Add($"Fixtures/ReleaseNotes/{fileName} differs from corpus/{area}/fixes/{fileName}");
        }

        foreach (string fileName in embedded.Keys)
        {
            if (!sources.Contains(fileName))
                differences.Add($"Fixtures/ReleaseNotes/{fileName} has no source in corpus/{area}/fixes/");
        }

        Assert.AreEqual(0, differences.Count, string.Join(Environment.NewLine, differences));
    }
}
