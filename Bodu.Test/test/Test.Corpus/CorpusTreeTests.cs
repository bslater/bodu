// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

/// <summary>
/// Verifies <see cref="CorpusTree" />: finding the repository root, and checking embedded catalogues against their
/// sources in <c>corpus/</c>.
/// </summary>
[TestClass]
public sealed partial class CorpusTreeTests
{
    /// <summary>
    /// Creates an empty directory under the temporary directory, standing in for a repository root.
    /// </summary>
    /// <returns>The directory's path.</returns>
    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "bodu-corpus-tree-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Reads the sample catalogue this assembly embeds.
    /// </summary>
    /// <returns>The catalogue's bytes.</returns>
    private static byte[] EmbeddedSample()
    {
        string resource = typeof(CorpusTreeTests).Assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(".Fixtures.ReleaseNotes.sample-fixes.csv", StringComparison.Ordinal));

        using Stream stream = typeof(CorpusTreeTests).Assembly.GetManifestResourceStream(resource)!;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>
    /// Creates <c>corpus/&lt;area&gt;/fixes/</c> under a root and writes files into it.
    /// </summary>
    /// <param name="root">The stand-in repository root.</param>
    /// <param name="files">The file names and contents to write.</param>
    private static void WriteCorpus(string root, params (string FileName, byte[] Content)[] files)
    {
        string directory = Path.Combine(root, "corpus", "sample-area", "fixes");
        Directory.CreateDirectory(directory);
        foreach ((string fileName, byte[] content) in files)
            File.WriteAllBytes(Path.Combine(directory, fileName), content);
    }
}
