// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlAllocationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Characterizes the managed-heap allocation profile of every principal read and write path, recording a baseline that
/// allocation regressions and a future flat-model redesign can be measured against. Two contracts are pinned exactly —
/// the lexical reader and read-only element lookup allocate nothing — while the materializing pipelines are bounded by a
/// generous multiple of the input size.
/// </summary>
/// <remarks>
/// <para>
/// Allocation counts from <see cref="GC.GetAllocatedBytesForCurrentThread" /> are independent of CPU speed, so these
/// bounds are stable across machines. Each measurement runs the operation once to absorb one-time JIT and cache
/// initialization, then measures a second run.
/// </para>
/// <para>
/// The multiplier bounds are deliberately loose: they exist to catch order-of-magnitude regressions and to document the
/// current baseline. The read pipeline now materializes a structural node tree and the flat row index — the flattened
/// token-list intermediate has been removed — and still allocates on the order of forty times the input; collapsing the
/// remaining tree into the flat model would lower these further, at which point the bounds should be tightened.
/// </para>
/// </remarks>
[TestClass]
public sealed class TomlAllocationTests
{
    /// <summary>
    /// The number of bare key/value lines in the sample document used to measure allocation.
    /// </summary>
    private const int SampleLineCount = 500;

    /// <summary>
    /// Verifies that tokenizing a contiguous document with <see cref="Utf8TomlReader" /> performs no per-token heap
    /// allocation, so the lexical reader's cost is independent of document size for a flat document.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Utf8TomlReader_WhenTokenizingContiguousDocument_ShouldNotAllocatePerToken()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        _ = TokenizeSpan(bytes);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var count = TokenizeSpan(bytes);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(count > SampleLineCount, "The reader produced too few tokens to be a meaningful measurement.");
        Assert.IsTrue(allocated < 512, $"Tokenizing a {bytes.Length}-byte contiguous document allocated {allocated} bytes; the lexical reader should not allocate per token.");
    }

    /// <summary>
    /// Verifies that tokenizing a multi-segment <see cref="ReadOnlySequence{T}" /> copies the input into a single
    /// contiguous buffer exactly once, documenting that the reader does not yet read across segments without copying.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Utf8TomlReader_WhenTokenizingMultiSegmentSequence_ShouldCopyInputOnce()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        var sequence = BuildSegmented(bytes, chunkSize: 64);

        _ = TokenizeSequence(sequence);
        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = TokenizeSequence(sequence);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(allocated >= bytes.Length, $"A multi-segment sequence of {bytes.Length} bytes allocated only {allocated} bytes; a contiguous copy of the input was expected.");
        Assert.IsTrue(allocated < bytes.Length * 4L, $"Tokenizing the multi-segment sequence allocated {allocated} bytes, more than the single contiguous copy expected.");
    }

    /// <summary>
    /// Verifies that parsing a document into a <see cref="TomlDocument" /> stays within the recorded allocation baseline
    /// relative to the input size.
    /// </summary>
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

        AssertWithinBaseline(allocated, bytes.Length, multiple: 48);
    }

    /// <summary>
    /// Verifies that reading values out of a parsed <see cref="TomlDocument" /> through its element views performs no
    /// heap allocation, confirming the flat row index is exposed without materializing node objects.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlDocument_WhenLookingUpProperties_ShouldNotAllocate()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        var keys = BuildKeys(SampleLineCount);
        using var document = TomlDocument.Parse(bytes);

        _ = SumByLookup(document, keys);
        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = SumByLookup(document, keys);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(allocated < 512, $"Looking up {keys.Length} properties allocated {allocated} bytes; element views should not allocate on read.");
    }

    /// <summary>
    /// Verifies that parsing a document into a mutable <see cref="TomlNode" /> tree stays within the recorded allocation
    /// baseline relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlNode_WhenParsing_ShouldStayWithinAllocationBaseline()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        var allocated = Measure(() => { _ = TomlNode.Parse(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 48);
    }

    /// <summary>
    /// Verifies that writing a mutable <see cref="TomlNode" /> tree back to TOML stays within the recorded allocation
    /// baseline relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlNode_WhenWriting_ShouldStayWithinAllocationBaseline()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        var node = TomlNode.Parse(bytes)!;

        var allocated = Measure(() => { _ = node.ToUtf8Bytes(); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 32);
    }

    /// <summary>
    /// Verifies that deserializing a document into a dictionary stays within the recorded allocation baseline relative
    /// to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlSerializer_WhenDeserializing_ShouldStayWithinAllocationBaseline()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        var allocated = Measure(() => { _ = TomlSerializer.Deserialize<Dictionary<string, long>>(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 56);
    }

    /// <summary>
    /// Verifies that serializing a dictionary to TOML text stays within the recorded allocation baseline relative to the
    /// input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlSerializer_WhenSerializing_ShouldStayWithinAllocationBaseline()
    {
        var bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        var model = TomlSerializer.Deserialize<Dictionary<string, long>>(bytes);

        var allocated = Measure(() => { _ = TomlSerializer.Serialize(model); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 40);
    }

    /// <summary>
    /// Asserts that an operation's allocation stays within <paramref name="multiple" /> times the input size.
    /// </summary>
    /// <param name="allocated">The measured bytes allocated.</param>
    /// <param name="inputLength">The input size in bytes.</param>
    /// <param name="multiple">The allowed allocation multiple of the input size.</param>
    private static void AssertWithinBaseline(long allocated, int inputLength, int multiple)
    {
        var limit = (long)inputLength * multiple;
        Assert.IsTrue(allocated < limit, $"The operation allocated {allocated} bytes for a {inputLength}-byte input, exceeding the recorded baseline of {limit} bytes ({multiple}x).");
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
    /// Tokenizes a contiguous source with <see cref="Utf8TomlReader" /> without decoding values, returning the token
    /// count.
    /// </summary>
    /// <param name="bytes">The UTF-8 source to tokenize.</param>
    /// <returns>The number of tokens read.</returns>
    private static int TokenizeSpan(byte[] bytes)
    {
        var reader = new Utf8TomlReader(bytes, new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_0 });
        var count = 0;
        while (reader.Read())
            count++;

        return count;
    }

    /// <summary>
    /// Tokenizes a multi-segment sequence with <see cref="Utf8TomlReader" /> without decoding values, returning the
    /// token count.
    /// </summary>
    /// <param name="sequence">The UTF-8 source sequence to tokenize.</param>
    /// <returns>The number of tokens read.</returns>
    private static int TokenizeSequence(ReadOnlySequence<byte> sequence)
    {
        var reader = new Utf8TomlReader(in sequence, new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_0 });
        var count = 0;
        while (reader.Read())
            count++;

        return count;
    }

    /// <summary>
    /// Sums the integer value of every named property by looking each up through the document's element views.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <param name="keys">The property names to look up.</param>
    /// <returns>The sum of the looked-up values.</returns>
    private static long SumByLookup(TomlDocument document, string[] keys)
    {
        long sum = 0;
        foreach (var key in keys)
            sum += document.RootElement.GetProperty(key).GetInt64();

        return sum;
    }

    /// <summary>
    /// Builds the property names <c>key0</c>..<c>key(count-1)</c> matching <see cref="BuildFlatDocument" />.
    /// </summary>
    /// <param name="count">The number of names to build.</param>
    /// <returns>The property names.</returns>
    private static string[] BuildKeys(int count)
    {
        var keys = new string[count];
        for (var i = 0; i < count; i++)
            keys[i] = "key" + i.ToString(CultureInfo.InvariantCulture);

        return keys;
    }

    /// <summary>
    /// Splits the source bytes into a multi-segment <see cref="ReadOnlySequence{T}" /> of fixed-size chunks.
    /// </summary>
    /// <param name="bytes">The source bytes to segment.</param>
    /// <param name="chunkSize">The size of each segment.</param>
    /// <returns>The multi-segment sequence over the source.</returns>
    private static ReadOnlySequence<byte> BuildSegmented(byte[] bytes, int chunkSize)
    {
        var first = new Segment(bytes.AsMemory(0, Math.Min(chunkSize, bytes.Length)));
        var current = first;
        var offset = chunkSize;
        while (offset < bytes.Length)
        {
            var length = Math.Min(chunkSize, bytes.Length - offset);
            current = current.Append(bytes.AsMemory(offset, length));
            offset += length;
        }

        return new ReadOnlySequence<byte>(first, 0, current, current.Memory.Length);
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

    /// <summary>
    /// A linked <see cref="ReadOnlySequenceSegment{T}" /> used to assemble a multi-segment sequence over an array.
    /// </summary>
    private sealed class Segment
        : ReadOnlySequenceSegment<byte>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Segment" /> class over the supplied memory.
        /// </summary>
        /// <param name="memory">The memory this segment exposes.</param>
        public Segment(ReadOnlyMemory<byte> memory) =>
            Memory = memory;

        /// <summary>
        /// Appends a following segment over the supplied memory and links it after this one.
        /// </summary>
        /// <param name="memory">The memory the next segment exposes.</param>
        /// <returns>The appended segment.</returns>
        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }
}
