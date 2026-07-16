// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeAllocationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Characterizes the managed-heap allocation profile of the principal read and write paths, recording a baseline that
/// allocation regressions can be measured against — the same contract <c>TomlAllocationTests</c> pins for the TOML
/// package. The lexical reader is pinned near-zero; the materializing pipelines are bounded by a multiple of the
/// input size.
/// </summary>
/// <remarks>
/// Allocation counts from <see cref="GC.GetAllocatedBytesForCurrentThread" /> are independent of CPU speed, so these
/// bounds are stable across machines. Each measurement runs the operation once to absorb one-time JIT, pool, and
/// cache initialization, then measures a second run.
/// </remarks>
[TestClass]
public sealed class BencodeAllocationTests
{
    /// <summary>
    /// The number of dictionary entries in the sample document used to measure allocation.
    /// </summary>
    private const int SampleEntryCount = 500;

    /// <summary>
    /// Verifies that tokenizing a document with <see cref="Utf8BencodeReader" /> performs no per-token heap
    /// allocation beyond the container frame list, so the reader's cost is independent of document size for a flat
    /// document.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Utf8BencodeReader_WhenTokenizingDocument_ShouldNotAllocatePerToken()
    {
        byte[] bytes = BuildFlatDictionary(SampleEntryCount);

        _ = Tokenize(bytes);
        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = Tokenize(bytes);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsGreaterThan(SampleEntryCount, count, "The reader produced too few tokens to be a meaningful measurement.");
        Assert.IsLessThan(512, allocated, $"Tokenizing a {bytes.Length}-byte document allocated {allocated} bytes; the lexical reader should not allocate per token.");
    }

    /// <summary>
    /// Verifies that serializing a dictionary stays within the recorded allocation baseline relative to the produced
    /// document size, pinning the pooled dictionary-frame buffering and range-based key handling.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void BencodeSerializer_WhenSerializing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = BuildFlatDictionary(SampleEntryCount);
        Dictionary<string, long> model = BencodeSerializer.Deserialize<Dictionary<string, long>>(bytes);

        long allocated = Measure(() => { _ = BencodeSerializer.Serialize(model); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 12);
    }

    /// <summary>
    /// Verifies that deserializing a dictionary stays within the recorded allocation baseline relative to the input
    /// size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void BencodeSerializer_WhenDeserializing_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = BuildFlatDictionary(SampleEntryCount);

        long allocated = Measure(() => { _ = BencodeSerializer.Deserialize<Dictionary<string, long>>(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 28);
    }

    /// <summary>
    /// Verifies that deserializing a list of dictionaries into POCO instances — the compiled-accessor, slot-buffered
    /// metadata path — stays within the recorded allocation baseline relative to the input size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void BencodeSerializer_WhenDeserializingObjectRows_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = BuildItemsDocument(SampleEntryCount);

        long allocated = Measure(() => { _ = BencodeSerializer.Deserialize<ItemsModel>(bytes); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 12);
    }

    /// <summary>
    /// Verifies that serializing a list of POCO instances stays within the recorded allocation baseline relative to
    /// the produced document size.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void BencodeSerializer_WhenSerializingObjectRows_ShouldStayWithinAllocationBaseline()
    {
        byte[] bytes = BuildItemsDocument(SampleEntryCount);
        ItemsModel model = BencodeSerializer.Deserialize<ItemsModel>(bytes);

        long allocated = Measure(() => { _ = BencodeSerializer.Serialize(model); });

        AssertWithinBaseline(allocated, bytes.Length, multiple: 17);
    }

    /// <summary>
    /// Builds a canonical flat Bencode dictionary of <paramref name="count" /> integer entries with zero-padded,
    /// bytewise-sorted keys.
    /// </summary>
    /// <param name="count">The number of entries.</param>
    /// <returns>The Bencode bytes.</returns>
    private static byte[] BuildFlatDictionary(int count)
    {
        var sb = new StringBuilder("d");
        for (int i = 0; i < count; i++)
            sb.Append("4:k").Append(i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)).Append('i').Append(i).Append('e');

        sb.Append('e');
        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Builds a Bencode document holding a list of <paramref name="count" /> small dictionaries, each binding to one
    /// POCO instance.
    /// </summary>
    /// <param name="count">The number of rows.</param>
    /// <returns>The Bencode bytes.</returns>
    private static byte[] BuildItemsDocument(int count)
    {
        var sb = new StringBuilder("d5:Itemsl");
        for (int i = 0; i < count; i++)
        {
            string name = "item" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sb.Append("d4:Flagi").Append(i % 2).Append("e2:Idi").Append(i).Append("e4:Name").Append(name.Length).Append(':').Append(name).Append('e');
        }

        sb.Append("ee");
        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Tokenizes a document with <see cref="Utf8BencodeReader" /> without decoding values, returning the token count.
    /// </summary>
    /// <param name="bytes">The Bencode bytes to tokenize.</param>
    /// <returns>The number of tokens read.</returns>
    private static int Tokenize(byte[] bytes)
    {
        var reader = new Utf8BencodeReader(bytes);
        int count = 0;
        while (reader.Read())
            count++;

        return count;
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
    /// A single POCO row bound from a list-of-dictionaries entry.
    /// </summary>
    private sealed class ItemModel
    {
        /// <summary>
        /// Gets or sets the flag.
        /// </summary>
        /// <value>The flag.</value>
        public long Flag { get; set; }

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
    }
}
