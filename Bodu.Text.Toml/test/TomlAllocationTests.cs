// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlAllocationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Characterizes the managed-heap allocation profile of the principal read paths: the lexical reader tokenizes without
/// allocating, while the normalized document pipeline establishes a recorded amplification baseline. Allocation counts
/// are independent of CPU speed, so these bounds are stable across machines and guard against allocation regressions.
/// </summary>
/// <remarks>
/// The bounds are deliberately generous so that minor runtime-version differences do not cause spurious failures; they
/// exist to catch order-of-magnitude regressions and to record the pre-redesign baseline against which a future
/// flat-model parse pipeline can be measured.
/// </remarks>
[TestClass]
public sealed class TomlAllocationTests
{
    /// <summary>
    /// The number of bare key/value lines in the sample document used to measure allocation.
    /// </summary>
    private const int SampleLineCount = 500;

    /// <summary>
    /// Verifies that tokenizing a document with <see cref="Utf8TomlReader" /> performs no per-token heap allocation, so
    /// the lexical reader's cost is independent of document size for a flat document.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Utf8TomlReader_WhenTokenizingFlatDocument_ShouldNotAllocatePerToken()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        _ = Tokenize(bytes);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var count = Tokenize(bytes);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(count > SampleLineCount, "The reader produced too few tokens to be a meaningful measurement.");
        Assert.IsTrue(allocated < 512, $"Tokenizing a {bytes.Length}-byte document allocated {allocated} bytes; the lexical reader should not allocate per token.");
    }

    /// <summary>
    /// Verifies that parsing a document into a <see cref="TomlDocument" /> stays within a recorded allocation baseline
    /// relative to the input size, guarding the normalized pipeline against further amplification.
    /// </summary>
    /// <remarks>
    /// The current pipeline materializes a structural node tree, a flattened token list, and the flat row index in turn,
    /// so it allocates on the order of forty times the input size; the bound is set at sixty-four times to leave
    /// headroom. A direct-to-flat parse pipeline would lower this substantially, at which point the bound should be
    /// tightened.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlDocument_WhenParsing_ShouldStayWithinAllocationBaseline()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        var allocated = Measure(() =>
        {
            using var document = TomlDocument.Parse(bytes);
            _ = document.RootElement;
        });

        Assert.IsTrue(allocated < bytes.Length * 64L, $"Parsing a {bytes.Length}-byte document allocated {allocated} bytes, exceeding the recorded baseline of {bytes.Length * 64L} bytes.");
    }

    /// <summary>
    /// Measures the managed bytes allocated by running <paramref name="action" /> once, after a warm-up run that absorbs
    /// one-time JIT and static-initialization costs.
    /// </summary>
    /// <param name="action">The operation to measure.</param>
    /// <returns>The bytes allocated on the current thread by a single run.</returns>
    private static long Measure(Action action)
    {
        action();
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>
    /// Tokenizes the source with <see cref="Utf8TomlReader" /> without decoding values, returning the token count.
    /// </summary>
    /// <param name="bytes">The UTF-8 source to tokenize.</param>
    /// <returns>The number of tokens read.</returns>
    private static int Tokenize(byte[] bytes)
    {
        var reader = new Utf8TomlReader(bytes, new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_0 });
        var count = 0;
        while (reader.Read())
            count++;

        return count;
    }

    /// <summary>
    /// Builds a flat TOML document of <paramref name="lines" /> bare key/value pairs.
    /// </summary>
    /// <param name="lines">The number of key/value lines to emit.</param>
    /// <returns>The TOML source text.</returns>
    private static string BuildFlatDocument(int lines)
    {
        var builder = new StringBuilder(lines * 16);
        for (var i = 0; i < lines; i++)
            builder.Append("key").Append(i).Append(" = ").Append(i).Append('\n');

        return builder.ToString();
    }
}
