// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Represents a space-efficient probabilistic set that tests whether an element <em>might</em> be a member. Membership
/// queries can produce false positives but never false negatives: an element that was added is always reported as
/// present, while an element that was never added is occasionally misreported as present.
/// </summary>
/// <typeparam name="T">The type of elements tracked by the filter. Must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// The filter is sized from the constructor arguments using the standard Bloom formulas: the bit count is
/// <c>m = ⌈−n·ln(p) / ln(2)²⌉</c> and the hash count is <c>k = max(1, round(m/n·ln 2))</c>, where <c>n</c> is
/// <see cref="ExpectedItems" /> and <c>p</c> is <see cref="DesignFalsePositiveRate" />. Once more than <c>n</c>
/// elements have been added the observed false-positive rate rises above <c>p</c>;
/// <see cref="EstimatedFalsePositiveRate" /> reports the rate implied by the current fill.
/// </para>
/// <para>
/// Elements are hashed via the supplied <see cref="IEqualityComparer{T}" /> (or
/// <see cref="EqualityComparer{T}.Default" />): the comparer's 32-bit
/// <see cref="IEqualityComparer{T}.GetHashCode(T)" /> is expanded through a deterministic SplitMix64-style avalanche
/// into two 64-bit values combined by Kirsch–Mitzenmacher double hashing. All entropy therefore derives from the 32-bit
/// comparer hash — standard practice for comparer-based sketches — which bounds the achievable false-positive floor by
/// the collision rate of that hash.
/// </para>
/// <para>
/// The expansion is platform-stable, but the comparer may not be: <see cref="string.GetHashCode()" /> is randomized per
/// process, so exported state for <see cref="string" /> elements (or any type with a randomized hash) is only
/// meaningful when re-imported within the same process, or when a custom comparer with a stable hash is used.
/// </para>
/// <para>
/// <see cref="UnionWith" /> merges another filter into this one by bitwise OR. The two filters must be compatible — the
/// same <see cref="BitCount" />, the same <see cref="HashCount" />, and the same (or equal) comparer — which in
/// practice means they were constructed with the same parameters. After the merge this filter reports an element as
/// possibly present when either source filter would have.
/// </para>
/// <para>
/// The export format produced by <see cref="TryExport" /> / <see cref="Export" /> and consumed by <see cref="Import" />
/// is an opaque, version-checked snapshot, not a wire contract; its layout may change between library versions with a
/// corresponding version-byte bump.
/// </para>
/// <para>
/// <see cref="BloomFilter{T}" /> is not thread-safe. Concurrent reads and writes require external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Track ~10,000 visitor ids with a 1% false-positive budget.
/// var seen = new BloomFilter<int>(expectedItems: 10_000, falsePositiveRate: 0.01);
///
/// seen.Add(42);
///
/// Console.WriteLine(seen.MightContain(42));   // True  — added items are always found
/// Console.WriteLine(seen.MightContain(4711)); // False (usually) — may rarely be a false positive
///]]>
/// </code>
/// </example>
[DebuggerDisplay("BitCount = {BitCount}, HashCount = {HashCount}, EstimatedFalsePositiveRate = {EstimatedFalsePositiveRate}")]
[DebuggerTypeProxy(typeof(BloomFilterDebugView<>))]
public sealed class BloomFilter<T>
    where T : notnull
{
    /// <summary>The single export format version this library version writes and accepts.</summary>
    private const byte ExportFormatVersion = 1;

    /// <summary>The size, in bytes, of the export header (version byte + bit count + hash count + expected items + rate bits).</summary>
    private const int ExportHeaderByteCount = 21;

    /// <summary>The largest supported bit count; keeps word-count arithmetic within <see cref="int" /> range.</summary>
    private const int MaxBitCount = int.MaxValue - 63;

    /// <summary>The largest hash count the sizing constructor can derive. The constructor computes <c>k = max(1, round(m/n·ln 2)) ≈ round(log₂(1/p))</c>, which peaks at the smallest representable positive rate (<see cref="double.Epsilon" /> = 2⁻¹⁰⁷⁴): <c>round(1074 + ln 2) = 1075</c>. <see cref="Import" /> rejects any snapshot claiming more, bounding the per-operation probe cost for hostile snapshots.</summary>
    private const int MaxHashCount = 1075;

    /// <summary>The equality comparer supplying element hash codes.</summary>
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>The bit array backing the filter, packed 64 bits per word.</summary>
    private readonly ulong[] _words;

    /// <summary>The number of addressable bits (<c>m</c>).</summary>
    private readonly int _bitCount;

    /// <summary>The number of bit positions probed per element (<c>k</c>).</summary>
    private readonly int _hashCount;

    /// <summary>The expected element count (<c>n</c>) the filter was sized for.</summary>
    private readonly int _expectedItems;

    /// <summary>The false-positive rate (<c>p</c>) the filter was sized for.</summary>
    private readonly double _designFalsePositiveRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="BloomFilter{T}" /> class sized for the expected element count and
    /// target false-positive rate, using the default equality comparer.
    /// </summary>
    /// <param name="expectedItems">The number of distinct elements the filter is expected to hold.</param>
    /// <param name="falsePositiveRate">The target false-positive probability at the expected fill.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="expectedItems" /> ≤ 0, <paramref name="falsePositiveRate" /> is outside the exclusive interval
    /// (0, 1), or the combination requires more than the maximum supported number of bits.
    /// </exception>
    public BloomFilter(int expectedItems, double falsePositiveRate)
        : this(expectedItems, falsePositiveRate, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BloomFilter{T}" /> class sized for the expected element count and
    /// target false-positive rate, using the specified equality comparer.
    /// </summary>
    /// <param name="expectedItems">The number of distinct elements the filter is expected to hold.</param>
    /// <param name="falsePositiveRate">The target false-positive probability at the expected fill.</param>
    /// <param name="comparer">
    /// The equality comparer whose hash codes drive bit selection. If <see langword="null" />, the default equality
    /// comparer for <typeparamref name="T" /> is used.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="expectedItems" /> ≤ 0, <paramref name="falsePositiveRate" /> is outside the exclusive interval
    /// (0, 1), or the combination requires more than the maximum supported number of bits.
    /// </exception>
    public BloomFilter(int expectedItems, double falsePositiveRate, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfZeroOrNegative(expectedItems);
        ThrowHelper.ThrowIfOutOfRange(falsePositiveRate, 0.0, 1.0, inclusive: false);

        var requiredBits = Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
        if (requiredBits > MaxBitCount) throw new ArgumentOutOfRangeException(nameof(expectedItems), string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Arg_OutOfRange_BloomFilterTooLarge, MaxBitCount));

        _expectedItems = expectedItems;
        _designFalsePositiveRate = falsePositiveRate;
        _bitCount = Math.Max(1, (int)requiredBits);
        _hashCount = Math.Max(1, (int)Math.Round(_bitCount / (double)expectedItems * Math.Log(2)));
        _words = new ulong[(_bitCount + 63) >> 6];
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BloomFilter{T}" /> class from previously validated state. Used by
    /// <see cref="Import" />; performs no validation of its own.
    /// </summary>
    /// <param name="expectedItems">The expected element count recorded in the imported state.</param>
    /// <param name="designFalsePositiveRate">The design false-positive rate recorded in the imported state.</param>
    /// <param name="bitCount">The number of addressable bits.</param>
    /// <param name="hashCount">The number of bit positions probed per element.</param>
    /// <param name="words">The backing bit array; ownership transfers to the new instance.</param>
    /// <param name="comparer">The equality comparer supplying element hash codes.</param>
    private BloomFilter(int expectedItems, double designFalsePositiveRate, int bitCount, int hashCount, ulong[] words, IEqualityComparer<T> comparer)
    {
        _expectedItems = expectedItems;
        _designFalsePositiveRate = designFalsePositiveRate;
        _bitCount = bitCount;
        _hashCount = hashCount;
        _words = words;
        _comparer = comparer;
    }

    /// <summary>
    /// Gets the number of addressable bits in the filter (<c>m</c>).
    /// </summary>
    /// <value>The bit count derived from the constructor arguments; always at least 1.</value>
    public int BitCount => _bitCount;

    /// <summary>
    /// Gets the equality comparer whose hash codes drive bit selection.
    /// </summary>
    /// <value>
    /// The <see cref="IEqualityComparer{T}" /> instance supplied at construction, or the default comparer.
    /// </value>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the false-positive probability the filter was sized for.
    /// </summary>
    /// <value>
    /// The <c>falsePositiveRate</c> constructor argument. The observed rate approaches this value as the element count
    /// approaches <see cref="ExpectedItems" /> and exceeds it beyond that point.
    /// </value>
    public double DesignFalsePositiveRate => _designFalsePositiveRate;

    /// <summary>
    /// Gets the false-positive probability implied by the current fill of the filter.
    /// </summary>
    /// <value>
    /// The probability that <see cref="MightContain" /> reports a never-added element as present, computed as
    /// <c>(setBits / m)^k</c> from the current bit density. Zero for an empty filter.
    /// </value>
    /// <remarks>
    /// The value is recomputed on each access by counting the set bits, an O(<see cref="BitCount" /> / 64) scan.
    /// </remarks>
    public double EstimatedFalsePositiveRate
    {
        get
        {
            var setBits = 0;
            foreach (var word in _words)
                setBits += BitOperations.PopCount(word);

            return Math.Pow((double)setBits / _bitCount, _hashCount);
        }
    }

    /// <summary>
    /// Gets the number of distinct elements the filter was sized for (<c>n</c>).
    /// </summary>
    /// <value>The <c>expectedItems</c> constructor argument.</value>
    public int ExpectedItems => _expectedItems;

    /// <summary>
    /// Gets the number of bit positions probed per element (<c>k</c>).
    /// </summary>
    /// <value>The hash count derived from the constructor arguments; always at least 1.</value>
    public int HashCount => _hashCount;

    /// <summary>
    /// Creates a <see cref="BloomFilter{T}" /> from state previously produced by <see cref="TryExport" /> or
    /// <see cref="Export" />.
    /// </summary>
    /// <param name="source">The exported filter state.</param>
    /// <param name="comparer">
    /// The equality comparer to associate with the imported filter. Must be the comparer (or an equal comparer) the
    /// exporting filter used, or membership results are undefined. If <see langword="null" />, the default equality
    /// comparer for <typeparamref name="T" /> is used.
    /// </param>
    /// <returns>A new filter whose membership state matches the exported filter.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> is truncated, carries an unsupported format version, or does not describe a valid
    /// filter.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The snapshot's hash count exceeds the largest value the sizing constructor can produce (1075, the value implied
    /// by the smallest representable false-positive rate).
    /// </exception>
    /// <remarks>
    /// <para>
    /// The comparer is not part of the exported state; the caller must re-supply it. For element types with
    /// process-randomized hash codes (notably <see cref="string" /> under the default comparer), an export is only
    /// meaningful within the process that produced it.
    /// </para>
    /// <para>
    /// The snapshot's hash count is bounded at import so a hostile or corrupted snapshot cannot inflate the
    /// per-operation cost of <see cref="Add" /> and <see cref="MightContain" /> (both are O(<see cref="HashCount" />)).
    /// </para>
    /// </remarks>
    public static BloomFilter<T> Import(ReadOnlySpan<byte> source, IEqualityComparer<T>? comparer = null)
    {
        if (source.Length < ExportHeaderByteCount) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_BloomFilterImportData, nameof(source));
        if (source[0] != ExportFormatVersion) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Arg_Invalid_BloomFilterImportVersion, source[0]), nameof(source));

        var bitCount = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(1, 4));
        var hashCount = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(5, 4));
        var expectedItems = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(9, 4));
        var designRate = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(source.Slice(13, 8)));

        if (bitCount <= 0 || bitCount > MaxBitCount || hashCount <= 0 || expectedItems <= 0 || !(designRate > 0.0 && designRate < 1.0))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_BloomFilterImportData, nameof(source));
        if (hashCount > MaxHashCount)
            throw new ArgumentOutOfRangeException(nameof(source), string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Arg_OutOfRange_BloomFilterImportHashCount, hashCount, MaxHashCount));

        var wordCount = (bitCount + 63) >> 6;
        if (source.Length != ExportHeaderByteCount + ((long)wordCount * 8))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_BloomFilterImportData, nameof(source));

        var words = new ulong[wordCount];
        for (var i = 0; i < wordCount; i++)
            words[i] = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(ExportHeaderByteCount + (i * 8), 8));

        return new BloomFilter<T>(expectedItems, designRate, bitCount, hashCount, words, comparer ?? EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Adds an element to the filter. After the call, <see cref="MightContain" /> is guaranteed to return
    /// <see langword="true" /> for the element.
    /// </summary>
    /// <param name="item">The element to add. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Adding the same element again is a no-op on the bit array. Elements cannot be removed; use <see cref="Clear" />
    /// to reset the entire filter.
    /// </remarks>
    public void Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        ProbabilisticHashing.DeriveHashPair(item, _comparer, out var h1, out var h2);

        // Incremental double hashing: g wraps mod 2^64 exactly as h1 + i·h2 does, so the probe sequence is
        // bit-identical to the multiplicative form (Export/Import compatibility) without a per-probe multiply.
        // The modulo must stay 64-bit: reducing h1/h2 first would lose the 2^64 wrap and change the sequence.
        var g = h1;
        for (var i = 0; i < _hashCount; i++)
        {
            var index = (int)(g % (ulong)_bitCount);
            _words[index >> 6] |= 1UL << (index & 63);
            g += h2;
        }
    }

    /// <summary>
    /// Removes all elements from the filter by clearing every bit.
    /// </summary>
    public void Clear() =>
        Array.Clear(_words);

    /// <summary>
    /// Writes the filter state to <paramref name="destination" />, which must be at least
    /// <see cref="GetExportByteCount" /> bytes long.
    /// </summary>
    /// <param name="destination">The buffer that receives the exported state.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="destination" /> is shorter than <see cref="GetExportByteCount" /> bytes.
    /// </exception>
    public void Export(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, GetExportByteCount());

        ExportCore(destination);
    }

    /// <summary>
    /// Returns the number of bytes required to export the filter state via <see cref="TryExport" /> or
    /// <see cref="Export" />.
    /// </summary>
    /// <returns>The exact export size in bytes for this filter's configuration.</returns>
    public int GetExportByteCount() =>
        ExportHeaderByteCount + (_words.Length * 8);

    /// <summary>
    /// Determines whether the element might be a member of the filter.
    /// </summary>
    /// <param name="item">The element to test. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="false" /> when the element was definitely never added; <see langword="true" /> when the element
    /// was added or is a false positive (with probability approximated by <see cref="EstimatedFalsePositiveRate" />).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public bool MightContain(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        ProbabilisticHashing.DeriveHashPair(item, _comparer, out var h1, out var h2);

        // Incremental double hashing; see Add for why the sequence matches the multiplicative form exactly.
        var g = h1;
        for (var i = 0; i < _hashCount; i++)
        {
            var index = (int)(g % (ulong)_bitCount);
            if ((_words[index >> 6] & (1UL << (index & 63))) == 0)
                return false;

            g += h2;
        }

        return true;
    }

    /// <summary>
    /// Attempts to write the filter state to <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">The buffer that receives the exported state.</param>
    /// <param name="bytesWritten">
    /// When the method returns <see langword="true" />, the number of bytes written (always
    /// <see cref="GetExportByteCount" />); otherwise 0.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="destination" /> is large enough and the state was written;
    /// otherwise <see langword="false" />.
    /// </returns>
    public bool TryExport(Span<byte> destination, out int bytesWritten)
    {
        var byteCount = GetExportByteCount();
        if (destination.Length < byteCount)
        {
            bytesWritten = 0;
            return false;
        }

        ExportCore(destination);
        bytesWritten = byteCount;
        return true;
    }

    /// <summary>
    /// Merges another compatible filter into this one so that this filter subsequently reports an element as possibly
    /// present when either source filter would have.
    /// </summary>
    /// <param name="other">The filter to merge into this one. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="other" /> is incompatible: its <see cref="BitCount" />, <see cref="HashCount" />, or comparer
    /// differs from this filter's.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Compatibility requires identical <see cref="BitCount" /> and <see cref="HashCount" /> and the same or an equal
    /// comparer instance — in practice, filters constructed with the same parameters. The other filter is not modified.
    /// Merging raises this filter's bit density and therefore its <see cref="EstimatedFalsePositiveRate" />.
    /// </para>
    /// <para>
    /// Comparer identity is decided by <see cref="object.Equals(object)" /> (after a reference check). Two distinct
    /// instances of a custom comparer type that does not override <see cref="object.Equals(object)" /> compare unequal
    /// even when behaviourally identical, and the merge is rejected. Share a single comparer instance between filters
    /// that will be merged, or override <c>Equals</c> (and <c>GetHashCode</c>) on the comparer type.
    /// </para>
    /// </remarks>
    public void UnionWith(BloomFilter<T> other)
    {
        ThrowHelper.ThrowIfNull(other);
        if (!IsCompatibleWith(other)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_IncompatibleBloomFilter, nameof(other));

        for (var i = 0; i < _words.Length; i++)
            _words[i] |= other._words[i];
    }

    /// <summary>
    /// Writes the export layout into a destination already validated to be large enough: the version byte, then the
    /// little-endian bit count, hash count, expected items, and design-rate bits, followed by the little-endian words.
    /// </summary>
    /// <param name="destination">
    /// The buffer that receives the exported state; at least <see cref="GetExportByteCount" /> bytes.
    /// </param>
    private void ExportCore(Span<byte> destination)
    {
        destination[0] = ExportFormatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(1, 4), _bitCount);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(5, 4), _hashCount);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(9, 4), _expectedItems);
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(13, 8), BitConverter.DoubleToInt64Bits(_designFalsePositiveRate));

        for (var i = 0; i < _words.Length; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(ExportHeaderByteCount + (i * 8), 8), _words[i]);
    }

    /// <summary>
    /// Determines whether <paramref name="other" /> shares this filter's geometry and comparer, making a bitwise merge
    /// meaningful.
    /// </summary>
    /// <param name="other">The candidate filter; never <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when the bit counts, hash counts, and comparers match; otherwise
    /// <see langword="false" />.
    /// </returns>
    private bool IsCompatibleWith(BloomFilter<T> other) =>
        _bitCount == other._bitCount
        && _hashCount == other._hashCount
        && (ReferenceEquals(_comparer, other._comparer) || _comparer.Equals(other._comparer));
}
