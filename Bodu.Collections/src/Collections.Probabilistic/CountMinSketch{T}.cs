// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketch{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Represents a space-efficient probabilistic frequency sketch that estimates how many times an element has been added.
/// Estimates can overestimate but never underestimate: <see cref="EstimateCount" /> always returns at least the true
/// count of the queried element, and with probability at least <c>1 − δ</c> returns at most the true count plus
/// <c>ε · TotalCount</c>.
/// </summary>
/// <typeparam name="T">The type of elements counted by the sketch. Must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// The sketch is sized from the constructor arguments using the standard count-min formulas: the width is
/// <c>w = ⌈e/ε⌉</c> counters per row and the depth is <c>d = ⌈ln(1/δ)⌉</c> rows, where <c>ε</c> is
/// <see cref="Epsilon" /> (the additive error factor) and <c>δ</c> is <see cref="Delta" /> (the probability of
/// exceeding it). The counters are stored in a single flat <c>d·w</c> array, row-major, for cache locality across the
/// per-row probes.
/// </para>
/// <para>
/// Elements are hashed via the supplied <see cref="IEqualityComparer{T}" /> (or
/// <see cref="EqualityComparer{T}.Default" />): the comparer's 32-bit
/// <see cref="IEqualityComparer{T}.GetHashCode(T)" /> is expanded through a deterministic SplitMix64-style avalanche
/// into two 64-bit values combined by Kirsch–Mitzenmacher double hashing, one probe per row. All entropy therefore
/// derives from the 32-bit comparer hash — standard practice for comparer-based sketches — which bounds the achievable
/// error floor by the collision rate of that hash: two elements with equal comparer hashes share every counter.
/// </para>
/// <para>
/// The expansion is platform-stable, but the comparer may not be: <see cref="string.GetHashCode()" /> is randomized per
/// process, so exported state for <see cref="string" /> elements (or any type with a randomized hash) is only
/// meaningful when re-imported within the same process, or when a custom comparer with a stable hash is used.
/// </para>
/// <para>
/// Counts can only grow. Negative updates are not supported because a removal can drive a shared counter below another
/// element's true count, breaking the never-underestimate guarantee that makes the minimum-over-rows estimate sound.
/// Use <see cref="Clear" /> to reset the entire sketch.
/// </para>
/// <para>
/// The export format produced by <see cref="TryExport" /> / <see cref="Export" /> and consumed by <see cref="Import" />
/// is an opaque, version-checked snapshot, not a wire contract; its layout may change between library versions with a
/// corresponding version-byte bump.
/// </para>
/// <para>
/// <see cref="CountMinSketch{T}" /> is not thread-safe. Concurrent reads and writes require external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Track event frequencies with 1% additive error at 99% confidence.
/// var sketch = new CountMinSketch<string>(epsilon: 0.01, delta: 0.01);
///
/// sketch.Add("page-view");
/// sketch.Add("page-view", 41);
///
/// Console.WriteLine(sketch.EstimateCount("page-view")); // >= 42, usually exactly 42
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Width = {Width}, Depth = {Depth}, TotalCount = {TotalCount}")]
[DebuggerTypeProxy(typeof(CountMinSketchDebugView<>))]
public sealed class CountMinSketch<T>
    where T : notnull
{
    /// <summary>The single export format version this library version writes and accepts.</summary>
    private const byte ExportFormatVersion = 1;

    /// <summary>The size, in bytes, of the export header (version byte + width + depth + epsilon bits + delta bits + total count).</summary>
    private const int ExportHeaderByteCount = 33;

    /// <summary>The largest supported counter-cell count; keeps the flat backing array and the export byte count within <see cref="int" /> range.</summary>
    private const int MaxCellCount = (int.MaxValue - ExportHeaderByteCount) / 8;

    /// <summary>The equality comparer supplying element hash codes.</summary>
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>The counter cells, stored row-major as a flat <c>depth · width</c> array for cache locality.</summary>
    private readonly long[] _cells;

    /// <summary>The number of counters per row (<c>w</c>).</summary>
    private readonly int _width;

    /// <summary>The number of rows (<c>d</c>), one probe per row.</summary>
    private readonly int _depth;

    /// <summary>The additive error factor (<c>ε</c>) the sketch was sized for.</summary>
    private readonly double _epsilon;

    /// <summary>The error probability (<c>δ</c>) the sketch was sized for.</summary>
    private readonly double _delta;

    /// <summary>The running sum of all added count magnitudes.</summary>
    private long _totalCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountMinSketch{T}" /> class sized for the target additive error and
    /// error probability, using the default equality comparer.
    /// </summary>
    /// <param name="epsilon">
    /// The additive error factor: estimates exceed the true count by at most <c>ε · TotalCount</c> with probability at
    /// least <c>1 − δ</c>.
    /// </param>
    /// <param name="delta">The probability that an estimate exceeds the error bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="epsilon" /> or <paramref name="delta" /> is outside the exclusive interval (0, 1), or the
    /// combination requires more than the maximum supported number of counter cells.
    /// </exception>
    public CountMinSketch(double epsilon, double delta)
        : this(epsilon, delta, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CountMinSketch{T}" /> class sized for the target additive error and
    /// error probability, using the specified equality comparer.
    /// </summary>
    /// <param name="epsilon">
    /// The additive error factor: estimates exceed the true count by at most <c>ε · TotalCount</c> with probability at
    /// least <c>1 − δ</c>.
    /// </param>
    /// <param name="delta">The probability that an estimate exceeds the error bound.</param>
    /// <param name="comparer">
    /// The equality comparer whose hash codes drive counter selection. If <see langword="null" />, the default equality
    /// comparer for <typeparamref name="T" /> is used.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="epsilon" /> or <paramref name="delta" /> is outside the exclusive interval (0, 1), or the
    /// combination requires more than the maximum supported number of counter cells.
    /// </exception>
    public CountMinSketch(double epsilon, double delta, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfOutOfRange(epsilon, 0.0, 1.0, inclusive: false);
        ThrowHelper.ThrowIfOutOfRange(delta, 0.0, 1.0, inclusive: false);

        var requiredWidth = Math.Ceiling(Math.E / epsilon);
        var requiredDepth = Math.Max(1.0, Math.Ceiling(Math.Log(1.0 / delta)));
        if (requiredWidth * requiredDepth > MaxCellCount) throw new ArgumentOutOfRangeException(nameof(epsilon), string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Arg_OutOfRange_CountMinSketchTooLarge, MaxCellCount));

        _epsilon = epsilon;
        _delta = delta;
        _width = Math.Max(1, (int)requiredWidth);
        _depth = (int)requiredDepth;
        _cells = new long[_depth * _width];
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CountMinSketch{T}" /> class from previously validated state. Used
    /// by <see cref="Import" />; performs no validation of its own.
    /// </summary>
    /// <param name="epsilon">The additive error factor recorded in the imported state.</param>
    /// <param name="delta">The error probability recorded in the imported state.</param>
    /// <param name="width">The number of counters per row.</param>
    /// <param name="depth">The number of rows.</param>
    /// <param name="totalCount">The running sum of added count magnitudes recorded in the imported state.</param>
    /// <param name="cells">The flat counter array; ownership transfers to the new instance.</param>
    /// <param name="comparer">The equality comparer supplying element hash codes.</param>
    private CountMinSketch(double epsilon, double delta, int width, int depth, long totalCount, long[] cells, IEqualityComparer<T> comparer)
    {
        _epsilon = epsilon;
        _delta = delta;
        _width = width;
        _depth = depth;
        _totalCount = totalCount;
        _cells = cells;
        _comparer = comparer;
    }

    /// <summary>
    /// Gets the equality comparer whose hash codes drive counter selection.
    /// </summary>
    /// <value>
    /// The <see cref="IEqualityComparer{T}" /> instance supplied at construction, or the default comparer.
    /// </value>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the probability (<c>δ</c>) that an estimate exceeds the additive error bound.
    /// </summary>
    /// <value>
    /// The <c>delta</c> constructor argument. With probability at least <c>1 − δ</c>, an estimate is at most the true
    /// count plus <see cref="Epsilon" /> · <see cref="TotalCount" />.
    /// </value>
    public double Delta => _delta;

    /// <summary>
    /// Gets the number of counter rows (<c>d</c>), one probe per row.
    /// </summary>
    /// <value>The depth derived from the constructor arguments as <c>⌈ln(1/δ)⌉</c>; always at least 1.</value>
    public int Depth => _depth;

    /// <summary>
    /// Gets the additive error factor (<c>ε</c>) the sketch was sized for.
    /// </summary>
    /// <value>
    /// The <c>epsilon</c> constructor argument. Estimates exceed the true count by at most <c>ε</c> ·
    /// <see cref="TotalCount" /> with probability at least <c>1 − δ</c>.
    /// </value>
    public double Epsilon => _epsilon;

    /// <summary>
    /// Gets the running sum of all count magnitudes added to the sketch.
    /// </summary>
    /// <value>
    /// The sum of every <c>count</c> passed to <see cref="Add(T, long)" /> (with <see cref="Add(T)" /> contributing 1
    /// per call). <see cref="MergeWith" /> adds the other sketch's total; <see cref="Clear" /> resets it to zero.
    /// </value>
    /// <remarks>
    /// This is the <c>N</c> in the error bound: an estimate exceeds the true count by at most <see cref="Epsilon" /> ·
    /// <see cref="TotalCount" /> with probability at least 1 − <see cref="Delta" />.
    /// </remarks>
    public long TotalCount => _totalCount;

    /// <summary>
    /// Gets the number of counters per row (<c>w</c>).
    /// </summary>
    /// <value>The width derived from the constructor arguments as <c>⌈e/ε⌉</c>; always at least 1.</value>
    public int Width => _width;

    /// <summary>
    /// Creates a <see cref="CountMinSketch{T}" /> from state previously produced by <see cref="TryExport" /> or
    /// <see cref="Export" />.
    /// </summary>
    /// <param name="source">The exported sketch state.</param>
    /// <param name="comparer">
    /// The equality comparer to associate with the imported sketch. Must be the comparer (or an equal comparer) the
    /// exporting sketch used, or estimates are undefined. If <see langword="null" />, the default equality comparer for
    /// <typeparamref name="T" /> is used.
    /// </param>
    /// <returns>A new sketch whose counter state matches the exported sketch.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> is truncated, carries an unsupported format version, or does not describe a valid
    /// sketch.
    /// </exception>
    /// <remarks>
    /// The comparer is not part of the exported state; the caller must re-supply it. For element types with
    /// process-randomized hash codes (notably <see cref="string" /> under the default comparer), an export is only
    /// meaningful within the process that produced it.
    /// </remarks>
    public static CountMinSketch<T> Import(ReadOnlySpan<byte> source, IEqualityComparer<T>? comparer = null)
    {
        if (source.Length < ExportHeaderByteCount) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_CountMinSketchImportData, nameof(source));
        if (source[0] != ExportFormatVersion) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Arg_Invalid_CountMinSketchImportVersion, source[0]), nameof(source));

        var width = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(1, 4));
        var depth = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(5, 4));
        var epsilon = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(source.Slice(9, 8)));
        var delta = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(source.Slice(17, 8)));
        var totalCount = BinaryPrimitives.ReadInt64LittleEndian(source.Slice(25, 8));

        if (width <= 0 || depth <= 0 || (long)width * depth > MaxCellCount || totalCount < 0 || !(epsilon > 0.0 && epsilon < 1.0) || !(delta > 0.0 && delta < 1.0))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_CountMinSketchImportData, nameof(source));

        var cellCount = width * depth;
        if (source.Length != ExportHeaderByteCount + ((long)cellCount * 8))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_CountMinSketchImportData, nameof(source));

        var cells = new long[cellCount];
        for (var i = 0; i < cellCount; i++)
            cells[i] = BinaryPrimitives.ReadInt64LittleEndian(source.Slice(ExportHeaderByteCount + (i * 8), 8));

        return new CountMinSketch<T>(epsilon, delta, width, depth, totalCount, cells, comparer ?? EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Adds a single occurrence of an element to the sketch.
    /// </summary>
    /// <param name="item">The element to count. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="OverflowException">
    /// A probed counter or <see cref="TotalCount" /> would exceed <see cref="long.MaxValue" />.
    /// </exception>
    public void Add(T item) =>
        Add(item, 1);

    /// <summary>
    /// Adds the specified number of occurrences of an element to the sketch.
    /// </summary>
    /// <param name="item">The element to count. Must not be <see langword="null" />.</param>
    /// <param name="count">The number of occurrences to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> ≤ 0.</exception>
    /// <exception cref="OverflowException">
    /// A probed counter or <see cref="TotalCount" /> would exceed <see cref="long.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Only positive counts are accepted. A negative update could drive a counter shared with another element below
    /// that element's true count, breaking the never-underestimate guarantee <see cref="EstimateCount" /> relies on.
    /// Use <see cref="Clear" /> to reset the entire sketch instead.
    /// </para>
    /// <para>
    /// Counter additions are checked; when an <see cref="OverflowException" /> is thrown mid-update, counters probed
    /// before the failing one retain the added count and the sketch may subsequently overestimate more than usual.
    /// </para>
    /// </remarks>
    public void Add(T item, long count)
    {
        ThrowHelper.ThrowIfNull(item);
        ThrowHelper.ThrowIfZeroOrNegative(count);

        ProbabilisticHashing.DeriveHashPair(item, _comparer, out var h1, out var h2);

        // Incremental double hashing: g wraps mod 2^64 exactly as h1 + i·h2 does, so the probe sequence is
        // bit-identical to the multiplicative form (Export/Import compatibility) without a per-probe multiply.
        // The modulo must stay 64-bit: reducing h1/h2 first would lose the 2^64 wrap and change the sequence.
        var g = h1;
        for (var i = 0; i < _depth; i++)
        {
            var cell = (i * _width) + (int)(g % (ulong)_width);
            _cells[cell] = checked(_cells[cell] + count);
            g += h2;
        }

        _totalCount = checked(_totalCount + count);
    }

    /// <summary>
    /// Removes all counts from the sketch by zeroing every counter and resetting <see cref="TotalCount" />.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_cells);

        _totalCount = 0;
    }

    /// <summary>
    /// Estimates how many times an element has been added to the sketch.
    /// </summary>
    /// <param name="item">The element to estimate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// The minimum counter value across the element's row probes. The estimate is never less than the element's true
    /// count; with probability at least 1 − <see cref="Delta" /> it is at most the true count plus
    /// <see cref="Epsilon" /> · <see cref="TotalCount" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// An element that was never added can still report a positive estimate when its counters collide with added
    /// elements; the estimate is zero only when at least one of its counters was never touched.
    /// </remarks>
    public long EstimateCount(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        ProbabilisticHashing.DeriveHashPair(item, _comparer, out var h1, out var h2);

        // Incremental double hashing; see Add(T, long) for why the sequence matches the multiplicative form exactly.
        var g = h1;
        var estimate = long.MaxValue;
        for (var i = 0; i < _depth; i++)
        {
            var cell = (i * _width) + (int)(g % (ulong)_width);
            estimate = Math.Min(estimate, _cells[cell]);
            g += h2;
        }

        return estimate;
    }

    /// <summary>
    /// Writes the sketch state to <paramref name="destination" />, which must be at least
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
    /// Returns the number of bytes required to export the sketch state via <see cref="TryExport" /> or
    /// <see cref="Export" />.
    /// </summary>
    /// <returns>The exact export size in bytes for this sketch's configuration.</returns>
    public int GetExportByteCount() =>
        ExportHeaderByteCount + (_cells.Length * 8);

    /// <summary>
    /// Merges another compatible sketch into this one by adding its counters cell-wise, so that this sketch
    /// subsequently estimates the combined stream of both sources.
    /// </summary>
    /// <param name="other">The sketch to merge into this one. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="other" /> is incompatible: its <see cref="Width" />, <see cref="Depth" />, or comparer differs
    /// from this sketch's.
    /// </exception>
    /// <exception cref="OverflowException">
    /// A cell-wise sum or the combined <see cref="TotalCount" /> would exceed <see cref="long.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Compatibility requires identical <see cref="Width" /> and <see cref="Depth" /> and the same or an equal comparer
    /// instance — in practice, sketches constructed with the same parameters. The other sketch is not modified, and
    /// <see cref="TotalCount" /> becomes the sum of both totals. Merging a sketch with itself doubles every count.
    /// </para>
    /// <para>
    /// Cell additions are checked; when an <see cref="OverflowException" /> is thrown mid-merge, cells merged before
    /// the failing one retain the added counts and the sketch may subsequently overestimate more than usual.
    /// </para>
    /// <para>
    /// Comparer identity is decided by <see cref="object.Equals(object)" /> (after a reference check). Two distinct
    /// instances of a custom comparer type that does not override <see cref="object.Equals(object)" /> compare unequal
    /// even when behaviourally identical, and the merge is rejected. Share a single comparer instance between sketches
    /// that will be merged, or override <c>Equals</c> (and <c>GetHashCode</c>) on the comparer type.
    /// </para>
    /// </remarks>
    public void MergeWith(CountMinSketch<T> other)
    {
        ThrowHelper.ThrowIfNull(other);
        if (!IsCompatibleWith(other)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_IncompatibleCountMinSketch, nameof(other));

        for (var i = 0; i < _cells.Length; i++)
            _cells[i] = checked(_cells[i] + other._cells[i]);

        _totalCount = checked(_totalCount + other._totalCount);
    }

    /// <summary>
    /// Attempts to write the sketch state to <paramref name="destination" />.
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
    /// Writes the export layout into a destination already validated to be large enough: the version byte, then the
    /// little-endian width, depth, epsilon bits, delta bits, and total count, followed by the little-endian counter
    /// cells in row-major order.
    /// </summary>
    /// <param name="destination">
    /// The buffer that receives the exported state; at least <see cref="GetExportByteCount" /> bytes.
    /// </param>
    private void ExportCore(Span<byte> destination)
    {
        destination[0] = ExportFormatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(1, 4), _width);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(5, 4), _depth);
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(9, 8), BitConverter.DoubleToInt64Bits(_epsilon));
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(17, 8), BitConverter.DoubleToInt64Bits(_delta));
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(25, 8), _totalCount);

        for (var i = 0; i < _cells.Length; i++)
            BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(ExportHeaderByteCount + (i * 8), 8), _cells[i]);
    }

    /// <summary>
    /// Determines whether <paramref name="other" /> shares this sketch's geometry and comparer, making a cell-wise
    /// merge meaningful.
    /// </summary>
    /// <param name="other">The candidate sketch; never <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when the widths, depths, and comparers match; otherwise <see langword="false" />.
    /// </returns>
    private bool IsCompatibleWith(CountMinSketch<T> other) =>
        _width == other._width
        && _depth == other._depth
        && (ReferenceEquals(_comparer, other._comparer) || _comparer.Equals(other._comparer));
}
