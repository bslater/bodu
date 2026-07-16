// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlAllocationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Characterizes the managed-heap allocation profile of the principal parse and serialize paths, recording a baseline
/// that allocation regressions can be measured against — the same contract <c>TomlAllocationTests</c> and
/// <c>BencodeAllocationTests</c> pin for the sibling packages. The materializing pipelines are bounded by a multiple
/// of the input size.
/// </summary>
/// <remarks>
/// Allocation counts from <see cref="GC.GetAllocatedBytesForCurrentThread" /> are independent of CPU speed, so these
/// bounds are stable across machines. Each measurement runs the operation once to absorb one-time JIT and
/// static-initialization costs, then measures a second run.
/// </remarks>
[TestClass]
public sealed class YamlAllocationTests
{
    /// <summary>
    /// The number of mapping lines in the sample document used to measure allocation.
    /// </summary>
    private const int SampleLineCount = 500;

    /// <summary>
    /// Verifies that parsing a plain-scalar-heavy document into a <see cref="YamlDocument" /> stays within the
    /// recorded allocation baseline relative to the input size, pinning the allocation-free scalar resolver.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void YamlDocument_WhenParsingPlainScalars_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        long allocated = Measure(() =>
        {
            using var document = YamlDocument.Parse(bytes);
            _ = document.RootElement;
        });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 40);
    }

    /// <summary>
    /// Verifies that parsing a document of quoted string scalars stays within the recorded allocation baseline
    /// relative to the input size, pinning the single-copy quoted-scalar decode.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void YamlDocument_WhenParsingQuotedScalars_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildQuotedDocument(SampleLineCount));

        long allocated = Measure(() =>
        {
            using var document = YamlDocument.Parse(bytes);
            _ = document.RootElement;
        });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 36);
    }

    /// <summary>
    /// Verifies that deserializing a dictionary stays within the recorded allocation baseline relative to the input
    /// size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void YamlSerializer_WhenDeserializing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        string text = Encoding.UTF8.GetString(bytes);

        long allocated = Measure(() => { _ = YamlSerializer.Deserialize<Dictionary<string, long>>(text); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 56);
    }

    /// <summary>
    /// Verifies that serializing a dictionary stays within the recorded allocation baseline relative to the produced
    /// document size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void YamlSerializer_WhenSerializing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        Dictionary<string, long> model = YamlSerializer.Deserialize<Dictionary<string, long>>(Encoding.UTF8.GetString(bytes));

        long allocated = Measure(() => { _ = YamlSerializer.Serialize(model); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 20);
    }

    /// <summary>
    /// Builds a flat YAML mapping of <paramref name="count" /> integer-valued lines.
    /// </summary>
    /// <param name="count">The number of lines.</param>
    /// <returns>The YAML text.</returns>
    private static string BuildFlatDocument(int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
            sb.Append("key").Append(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)).Append(": ").Append(i).Append('\n');

        return sb.ToString();
    }

    /// <summary>
    /// Builds a flat YAML mapping of <paramref name="count" /> double-quoted string lines, each carrying an escape.
    /// </summary>
    /// <param name="count">The number of lines.</param>
    /// <returns>The YAML text.</returns>
    private static string BuildQuotedDocument(int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
            sb.Append("key").Append(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)).Append(": \"value\\t").Append(i).Append("\"\n");

        return sb.ToString();
    }

    /// <summary>
    /// Asserts that an operation's allocation stays within <paramref name="multiple" /> times the input size.
    /// </summary>
    /// <param name="allocated">The measured bytes allocated.</param>
    /// <param name="inputLength">The input size in bytes.</param>
    /// <param name="multiple">The allowed allocation multiple of the input size.</param>
    private static void AssertWithinBaseline(long allocated, int inputLength, int multiple)
    {
        long limit = (long)inputLength * multiple;
        Assert.IsLessThan(limit, allocated, $"The operation allocated {allocated} bytes for a {inputLength}-byte input, exceeding the recorded baseline of {limit} bytes ({multiple}x).");
    }

    /// <summary>
    /// Measures the managed bytes allocated by running <paramref name="action" /> once, after a warm-up run that
    /// absorbs one-time JIT and static-initialization costs.
    /// </summary>
    /// <param name="action">The operation to measure.</param>
    /// <returns>The bytes allocated on the current thread by a single run.</returns>
    private static long Measure(Action action)
    {
        action();
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
