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
/// current baseline. The structural parser reuses a per-depth scratch list for key paths, hands its row list to the
/// document without a final array copy, and packs every scalar into the row — value types unboxed, and a string as the
/// source span of its content, decoded on demand from the source the document retains in a pooled buffer. So
/// <c>TomlDocument.Parse</c> sits near eight times the input, the serializer's bind path near twenty-six, and the mutable
/// node DOM near twenty-two; parsing a string-valued document without reading the values stays in the single digits
/// because the strings are never decoded. The bind and node paths still build their own representations from the shared
/// store.
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
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        _ = TokenizeSpan(bytes);
        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = TokenizeSpan(bytes);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsGreaterThan(SampleLineCount, count, "The reader produced too few tokens to be a meaningful measurement.");
        Assert.IsLessThan(512, allocated, $"Tokenizing a {bytes.Length}-byte contiguous document allocated {allocated} bytes; the lexical reader should not allocate per token.");
    }

    /// <summary>
    /// Verifies that tokenizing a multi-segment <see cref="ReadOnlySequence{T}" /> copies the input into a single
    /// contiguous buffer exactly once, documenting that the reader does not yet read across segments without copying.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Utf8TomlReader_WhenTokenizingMultiSegmentSequence_ShouldCopyInputOnce()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        ReadOnlySequence<byte> sequence = BuildSegmented(bytes, chunkSize: 64);

        _ = TokenizeSequence(sequence);
        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = TokenizeSequence(sequence);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsGreaterThanOrEqualTo(bytes.Length, allocated, $"A multi-segment sequence of {bytes.Length} bytes allocated only {allocated} bytes; a contiguous copy of the input was expected.");
        Assert.IsLessThan(bytes.Length * 4L, allocated, $"Tokenizing the multi-segment sequence allocated {allocated} bytes, more than the single contiguous copy expected.");
    }

    /// <summary>
    /// Verifies that parsing a document into a <see cref="TomlDocument" /> stays within the recorded allocation baseline
    /// relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlDocument_WhenParsing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        long allocated = Measure(() =>
        {
            using var document = TomlDocument.Parse(bytes);
            _ = document.RootElement;
        });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 11);
    }

    /// <summary>
    /// Verifies that parsing a string-valued document into a <see cref="TomlDocument" /> without reading the values
    /// stays within the recorded allocation baseline, confirming string scalars are not decoded at parse time but only
    /// when read.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlDocument_WhenParsingStringValuesWithoutReading_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildStringDocument(SampleLineCount));

        long allocated = Measure(() =>
        {
            using var document = TomlDocument.Parse(bytes);
            _ = document.RootElement;
        });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 8);
    }

    /// <summary>
    /// Verifies that reading values out of a parsed <see cref="TomlDocument" /> through its element views performs no
    /// heap allocation, confirming the flat row index is exposed without materializing node objects.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlDocument_WhenLookingUpProperties_ShouldNotAllocate()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        string[] keys = BuildKeys(SampleLineCount);
        using var document = TomlDocument.Parse(bytes);

        _ = SumByLookup(document, keys);
        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = SumByLookup(document, keys);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsLessThan(512, allocated, $"Looking up {keys.Length} properties allocated {allocated} bytes; element views should not allocate on read.");
    }

    /// <summary>
    /// Verifies that parsing a document into a mutable <see cref="TomlNode" /> tree stays within the recorded allocation
    /// baseline relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlNode_WhenParsing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        long allocated = Measure(() => { _ = TomlNode.Parse(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 30);
    }

    /// <summary>
    /// Verifies that writing a mutable <see cref="TomlNode" /> tree back to TOML stays within the recorded allocation
    /// baseline relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlNode_WhenWriting_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        TomlNode node = TomlNode.Parse(bytes)!;

        long allocated = Measure(() => { _ = node.ToUtf8Bytes(); });

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
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));

        long allocated = Measure(() => { _ = TomlSerializer.Deserialize<Dictionary<string, long>>(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 36);
    }

    /// <summary>
    /// Verifies that serializing a dictionary to TOML text stays within the recorded allocation baseline relative to the
    /// input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlSerializer_WhenSerializing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildFlatDocument(SampleLineCount));
        Dictionary<string, long> model = TomlSerializer.Deserialize<Dictionary<string, long>>(bytes);

        long allocated = Measure(() => { _ = TomlSerializer.Serialize(model); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 40);
    }

    /// <summary>
    /// Verifies that deserializing an array of tables into POCO instances — the compiled-accessor metadata path —
    /// stays within the recorded allocation baseline relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlSerializer_WhenDeserializingObjectRows_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildItemsDocument(SampleLineCount));

        long allocated = Measure(() => { _ = TomlSerializer.Deserialize<ItemsModel>(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 22);
    }

    /// <summary>
    /// Verifies that serializing a list of POCO instances — the compiled-accessor metadata path — stays within the
    /// recorded allocation baseline relative to the produced document size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TomlSerializer_WhenSerializingObjectRows_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(BuildItemsDocument(SampleLineCount));
        ItemsModel model = TomlSerializer.Deserialize<ItemsModel>(bytes);

        long allocated = Measure(() => { _ = TomlSerializer.Serialize(model); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 30);
    }

    /// <summary>
    /// Builds a TOML document of <paramref name="count" /> array-of-table rows, each binding to one POCO instance.
    /// </summary>
    /// <param name="count">The number of rows.</param>
    /// <returns>The TOML text.</returns>
    private static string BuildItemsDocument(int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            sb.Append("[[Items]]\n");
            sb.Append("Id = ").Append(i).Append('\n');
            sb.Append("Name = \"item").Append(i).Append("\"\n");
            sb.Append("Flag = ").Append(i % 2 == 0 ? "true" : "false").Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// The root model for the POCO allocation baselines.
    /// </summary>
    private sealed class ItemsModel
    {
        /// <summary>
        /// Gets or sets the item rows.
        /// </summary>
        /// <value>The item rows.</value>
        public List<ItemModel> Items { get; set; } = [];
    }

    /// <summary>
    /// A single POCO row bound from an array-of-tables entry.
    /// </summary>
    private sealed class ItemModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the flag.
        /// </summary>
        /// <value>The flag.</value>
        public bool Flag { get; set; }
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
    /// Measures the managed bytes allocated by running <paramref name="action" /> once, after a warm-up run that absorbs
    /// one-time JIT and static-initialization costs.
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

    /// <summary>
    /// Tokenizes a contiguous source with <see cref="Utf8TomlReader" /> without decoding values, returning the token
    /// count.
    /// </summary>
    /// <param name="bytes">The UTF-8 source to tokenize.</param>
    /// <returns>The number of tokens read.</returns>
    private static int TokenizeSpan(byte[] bytes)
    {
        var reader = new Utf8TomlReader(bytes, new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_0 });
        int count = 0;
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
        int count = 0;
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
        foreach (string key in keys)
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
        string[] keys = new string[count];
        for (int i = 0; i < count; i++)
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
        Segment current = first;
        int offset = chunkSize;
        while (offset < bytes.Length)
        {
            int length = Math.Min(chunkSize, bytes.Length - offset);
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
        for (int i = 0; i < lines; i++)
            builder.Append("key").Append(i).Append(" = ").Append(i).Append('\n');

        return builder.ToString();
    }

    /// <summary>
    /// Builds a flat TOML document of <paramref name="lines" /> bare key/value pairs whose values are quoted strings,
    /// used to measure the read-only document's string handling.
    /// </summary>
    /// <param name="lines">The number of key/value lines to emit.</param>
    /// <returns>The TOML source text.</returns>
    private static string BuildStringDocument(int lines)
    {
        var builder = new StringBuilder(lines * 32);
        for (int i = 0; i < lines; i++)
            builder.Append("key").Append(i).Append(" = \"value ").Append(i).Append(" payload text\"\n");

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
