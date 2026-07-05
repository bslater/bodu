// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLog{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Represents a space-efficient probabilistic sketch that estimates the number of distinct elements added to it. The
/// estimate is approximate: it carries a relative standard error of about <c>1.04/√m</c>, where <c>m</c> is
/// <see cref="RegisterCount" />, but the sketch itself occupies only one byte per register regardless of how many
/// elements are observed.
/// </summary>
/// <typeparam name="T">The type of elements counted by the sketch. Must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// The sketch is sized by the <c>precision</c> constructor argument <c>b</c>: it allocates <c>m = 2^b</c> single-byte
/// registers, and <see cref="EstimateCardinality" /> carries a relative standard error of approximately <c>1.04/√m</c>
/// (see <see cref="StandardError" />). Each increment of <c>b</c> doubles the memory footprint and improves the error
/// by a factor of <c>√2</c>: <c>b = 4</c> uses 16 registers at ~26% error, while <c>b = 14</c> uses 16,384 registers at
/// ~0.81% error.
/// </para>
/// <para>
/// Elements are hashed via the supplied <see cref="IEqualityComparer{T}" /> (or
/// <see cref="EqualityComparer{T}.Default" />): the comparer's 32-bit
/// <see cref="IEqualityComparer{T}.GetHashCode(T)" /> is expanded through a deterministic SplitMix64-style avalanche
/// into the shared double-hash pair, of which only the first 64-bit value is consumed — HyperLogLog needs a single
/// well-avalanched hash, not a probe sequence, so the second value is deliberately unused. The top <c>b</c> bits select
/// a register and the remaining <c>64 − b</c> bits contribute their leading-zero rank. All entropy therefore derives
/// from the 32-bit comparer hash — standard practice for comparer-based sketches — which caps the number of
/// distinguishable elements at 2³² and bounds accuracy well below the theoretical HyperLogLog error on very large
/// cardinalities: two elements with equal comparer hashes are indistinguishable and count once.
/// </para>
/// <para>
/// The expansion is platform-stable, but the comparer may not be: <see cref="string.GetHashCode()" /> is randomized per
/// process, so exported state for <see cref="string" /> elements (or any type with a randomized hash) is only
/// meaningful when re-imported within the same process, or when a custom comparer with a stable hash is used.
/// </para>
/// <para>
/// <see cref="EstimateCardinality" /> applies the standard HyperLogLog small-range correction (linear counting) while
/// any register is still zero and the raw estimate is at most <c>2.5·m</c>. The classic large-range correction from the
/// original paper compensates for hash collisions in a <em>32-bit</em> hash space and is deliberately omitted: the
/// register pipeline here is 64-bit, where that correction does not apply. (The practical accuracy ceiling is instead
/// the 32-bit comparer entropy described above.)
/// </para>
/// <para>
/// <see cref="MergeWith" /> combines two compatible sketches by register-wise maximum, after which this sketch
/// estimates the number of distinct elements in the union of both source streams. Merging is lossless — merging
/// sketches of two streams yields exactly the sketch that observing the concatenated stream would have produced — and
/// elements present in both streams are not double-counted.
/// </para>
/// <para>
/// The export format produced by <see cref="TryExport" /> / <see cref="Export" /> and consumed by <see cref="Import" />
/// is an opaque, version-checked snapshot, not a wire contract; its layout may change between library versions with a
/// corresponding version-byte bump.
/// </para>
/// <para>
/// <see cref="HyperLogLog{T}" /> is not thread-safe. Concurrent reads and writes require external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Count distinct visitor ids with ~0.81% standard error (2^14 = 16,384 one-byte registers).
/// var distinct = new HyperLogLog<int>(precision: 14);
///
/// for (var i = 0; i < 100_000; i++)
///     distinct.Add(i % 25_000); // 25,000 distinct values, each added four times
///
/// Console.WriteLine(distinct.EstimateCardinality()); // ~25,000
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Precision = {Precision}, RegisterCount = {RegisterCount}, StandardError = {StandardError}")]
[DebuggerTypeProxy(typeof(HyperLogLogDebugView<>))]
public sealed class HyperLogLog<T>
    where T : notnull
{
    /// <summary>The single export format version this library version writes and accepts.</summary>
    private const byte ExportFormatVersion = 1;

    /// <summary>The size, in bytes, of the export header (version byte + precision byte).</summary>
    private const int ExportHeaderByteCount = 2;

    /// <summary>The smallest supported precision (<c>b</c>); yields 16 registers at ~26% standard error.</summary>
    private const int MinPrecision = 4;

    /// <summary>The largest supported precision (<c>b</c>); yields 262,144 registers at ~0.2% standard error.</summary>
    private const int MaxPrecision = 18;

    /// <summary>The equality comparer supplying element hash codes.</summary>
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>The registers, one byte each; a register holds the maximum observed rank for its hash bucket.</summary>
    private readonly byte[] _registers;

    /// <summary>The precision (<c>b</c>): the number of hash bits used to select a register.</summary>
    private readonly int _precision;

    /// <summary>
    /// Initializes a new instance of the <see cref="HyperLogLog{T}" /> class with the specified precision, using the
    /// default equality comparer.
    /// </summary>
    /// <param name="precision">The precision (<c>b</c>): the sketch allocates <c>2^b</c> one-byte registers.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="precision" /> is outside the inclusive interval [4, 18].
    /// </exception>
    /// <remarks>
    /// Precision trades memory for accuracy: the sketch uses <c>m = 2^b</c> registers and the estimate's relative
    /// standard error is approximately <c>1.04/√m</c>, so each extra bit of precision doubles the footprint and
    /// improves the error by <c>√2</c>.
    /// </remarks>
    public HyperLogLog(int precision)
        : this(precision, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HyperLogLog{T}" /> class with the specified precision, using the
    /// specified equality comparer.
    /// </summary>
    /// <param name="precision">The precision (<c>b</c>): the sketch allocates <c>2^b</c> one-byte registers.</param>
    /// <param name="comparer">
    /// The equality comparer whose hash codes drive register selection. If <see langword="null" />, the default
    /// equality comparer for <typeparamref name="T" /> is used.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="precision" /> is outside the inclusive interval [4, 18].
    /// </exception>
    /// <remarks>
    /// Precision trades memory for accuracy: the sketch uses <c>m = 2^b</c> registers and the estimate's relative
    /// standard error is approximately <c>1.04/√m</c>, so each extra bit of precision doubles the footprint and
    /// improves the error by <c>√2</c>.
    /// </remarks>
    public HyperLogLog(int precision, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfOutOfRange(precision, MinPrecision, MaxPrecision);

        _precision = precision;
        _registers = new byte[1 << precision];
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HyperLogLog{T}" /> class from previously validated state. Used by
    /// <see cref="Import" />; performs no validation of its own.
    /// </summary>
    /// <param name="precision">The precision recorded in the imported state.</param>
    /// <param name="registers">The register array; ownership transfers to the new instance.</param>
    /// <param name="comparer">The equality comparer supplying element hash codes.</param>
    private HyperLogLog(int precision, byte[] registers, IEqualityComparer<T> comparer)
    {
        _precision = precision;
        _registers = registers;
        _comparer = comparer;
    }

    /// <summary>
    /// Gets the equality comparer whose hash codes drive register selection.
    /// </summary>
    /// <value>
    /// The <see cref="IEqualityComparer{T}" /> instance supplied at construction, or the default comparer.
    /// </value>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the precision (<c>b</c>): the number of hash bits used to select a register.
    /// </summary>
    /// <value>The <c>precision</c> constructor argument; always within the inclusive interval [4, 18].</value>
    public int Precision => _precision;

    /// <summary>
    /// Gets the number of registers (<c>m</c>) backing the sketch.
    /// </summary>
    /// <value><c>2^b</c> where <c>b</c> is <see cref="Precision" />; each register occupies one byte.</value>
    public int RegisterCount => _registers.Length;

    /// <summary>
    /// Gets the relative standard error of <see cref="EstimateCardinality" /> implied by the precision.
    /// </summary>
    /// <value>
    /// <c>1.04/√m</c> where <c>m</c> is <see cref="RegisterCount" /> — approximately 0.26 at precision 4 and 0.0081 at
    /// precision 14.
    /// </value>
    /// <remarks>
    /// This is the asymptotic one-sigma relative error of the HyperLogLog estimator; roughly 65% of estimates fall
    /// within one standard error of the true cardinality and roughly 99% within three.
    /// </remarks>
    public double StandardError => 1.04 / Math.Sqrt(_registers.Length);

    /// <summary>
    /// Creates a <see cref="HyperLogLog{T}" /> from state previously produced by <see cref="TryExport" /> or
    /// <see cref="Export" />.
    /// </summary>
    /// <param name="source">The exported sketch state.</param>
    /// <param name="comparer">
    /// The equality comparer to associate with the imported sketch. Must be the comparer (or an equal comparer) the
    /// exporting sketch used, or estimates are undefined. If <see langword="null" />, the default equality comparer for
    /// <typeparamref name="T" /> is used.
    /// </param>
    /// <returns>A new sketch whose register state matches the exported sketch.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> is truncated, carries an unsupported format version, or does not describe a valid
    /// sketch.
    /// </exception>
    /// <remarks>
    /// The comparer is not part of the exported state; the caller must re-supply it. For element types with
    /// process-randomized hash codes (notably <see cref="string" /> under the default comparer), an export is only
    /// meaningful within the process that produced it.
    /// </remarks>
    public static HyperLogLog<T> Import(ReadOnlySpan<byte> source, IEqualityComparer<T>? comparer = null)
    {
        if (source.Length < ExportHeaderByteCount) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_HyperLogLogImportData, nameof(source));
        if (source[0] != ExportFormatVersion) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Arg_Invalid_HyperLogLogImportVersion, source[0]), nameof(source));

        int precision = source[1];
        if (precision < MinPrecision || precision > MaxPrecision)
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_HyperLogLogImportData, nameof(source));

        var registerCount = 1 << precision;
        if (source.Length != ExportHeaderByteCount + registerCount)
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_HyperLogLogImportData, nameof(source));

        // A register can never legitimately exceed the maximum rank for this precision; a larger value indicates
        // corruption and would silently skew every subsequent estimate.
        var maxRank = 64 - precision + 1;
        var payload = source.Slice(ExportHeaderByteCount, registerCount);
        for (var i = 0; i < payload.Length; i++)
        {
            if (payload[i] > maxRank)
                throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_HyperLogLogImportData, nameof(source));
        }

        return new HyperLogLog<T>(precision, payload.ToArray(), comparer ?? EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Adds an element to the sketch. Adding an element that hashes identically to one already observed leaves the
    /// sketch unchanged.
    /// </summary>
    /// <param name="item">The element to count. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    public void Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        ProbabilisticHashing.DeriveHashPair(item, _comparer, out var hash, out _);

        // The top b bits select the register; the remaining 64 − b bits contribute their leading-zero rank, capped at
        // 64 − b + 1 for the all-zero suffix.
        var index = (int)(hash >> (64 - _precision));
        var suffix = hash << _precision;
        var rank = (byte)Math.Min(BitOperations.LeadingZeroCount(suffix) + 1, 64 - _precision + 1);

        if (rank > _registers[index])
            _registers[index] = rank;
    }

    /// <summary>
    /// Removes all state from the sketch by zeroing every register, after which <see cref="EstimateCardinality" />
    /// returns 0.
    /// </summary>
    public void Clear() =>
        Array.Clear(_registers);

    /// <summary>
    /// Estimates the number of distinct elements added to the sketch.
    /// </summary>
    /// <returns>
    /// The estimated distinct-element count: the bias-corrected harmonic mean <c>α·m²/Σ2^(−reg)</c>, or the linear
    /// counting estimate <c>m·ln(m/V)</c> (<c>V</c> = number of zero registers) when the raw estimate is at most
    /// <c>2.5·m</c> and at least one register is still zero. An empty sketch returns exactly 0.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The estimate is unbiased with a relative standard error of approximately <see cref="StandardError" />. It is not
    /// monotonic in the true cardinality at fine granularity — adding one element can move the estimate by more or less
    /// than one — but it converges on the true count as the stream grows.
    /// </para>
    /// <para>
    /// The original algorithm's large-range correction is deliberately omitted: it compensates for collisions in a
    /// 32-bit hash space, whereas this implementation ranks a 64-bit hash. See the class remarks for the practical
    /// accuracy bound imposed by the 32-bit comparer hash.
    /// </para>
    /// </remarks>
    public double EstimateCardinality()
    {
        var m = _registers.Length;

        var inverseSum = 0.0;
        var zeroRegisters = 0;
        for (var i = 0; i < m; i++)
        {
            var register = _registers[i];
            inverseSum += 1.0 / (1UL << register);
            if (register == 0)
                zeroRegisters++;
        }

        var rawEstimate = GetAlpha(m) * m * (double)m / inverseSum;

        // Small-range correction: while any register is untouched and the raw estimate is small, linear counting over
        // the zero registers is more accurate than the harmonic-mean estimator.
        if (rawEstimate <= 2.5 * m && zeroRegisters > 0)
            return m * Math.Log((double)m / zeroRegisters);

        return rawEstimate;
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
        ExportHeaderByteCount + _registers.Length;

    /// <summary>
    /// Merges another compatible sketch into this one by taking the register-wise maximum, so that this sketch
    /// subsequently estimates the number of distinct elements in the union of both source streams.
    /// </summary>
    /// <param name="other">The sketch to merge into this one. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="other" /> is incompatible: its <see cref="Precision" /> or comparer differs from this sketch's.
    /// </exception>
    /// <remarks>
    /// Compatibility requires an identical <see cref="Precision" /> and the same or an equal comparer instance — in
    /// practice, sketches constructed with the same parameters. The other sketch is not modified. The merge is lossless
    /// and idempotent: elements observed by both sketches are not double-counted, and merging a sketch with itself
    /// leaves it unchanged.
    /// </remarks>
    public void MergeWith(HyperLogLog<T> other)
    {
        ThrowHelper.ThrowIfNull(other);
        if (!IsCompatibleWith(other)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_IncompatibleHyperLogLog, nameof(other));

        for (var i = 0; i < _registers.Length; i++)
        {
            if (other._registers[i] > _registers[i])
                _registers[i] = other._registers[i];
        }
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
    /// Returns the bias-correction constant <c>α</c> for a register count, per the published HyperLogLog table.
    /// </summary>
    /// <param name="registerCount">The number of registers (<c>m</c>); always a power of two of at least 16.</param>
    /// <returns>
    /// 0.673 for 16 registers, 0.697 for 32, 0.709 for 64, and <c>0.7213/(1 + 1.079/m)</c> for 128 or more.
    /// </returns>
    private static double GetAlpha(int registerCount) =>
        registerCount switch
        {
            16 => 0.673,
            32 => 0.697,
            64 => 0.709,
            _ => 0.7213 / (1.0 + (1.079 / registerCount)),
        };

    /// <summary>
    /// Writes the export layout into a destination already validated to be large enough: the version byte, then the
    /// precision byte, followed by the registers verbatim.
    /// </summary>
    /// <param name="destination">
    /// The buffer that receives the exported state; at least <see cref="GetExportByteCount" /> bytes.
    /// </param>
    private void ExportCore(Span<byte> destination)
    {
        destination[0] = ExportFormatVersion;
        destination[1] = (byte)_precision;

        _registers.CopyTo(destination.Slice(ExportHeaderByteCount, _registers.Length));
    }

    /// <summary>
    /// Determines whether <paramref name="other" /> shares this sketch's precision and comparer, making a register-wise
    /// merge meaningful.
    /// </summary>
    /// <param name="other">The candidate sketch; never <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when the precisions and comparers match; otherwise <see langword="false" />.
    /// </returns>
    private bool IsCompatibleWith(HyperLogLog<T> other) =>
        _precision == other._precision
        && (ReferenceEquals(_comparer, other._comparer) || _comparer.Equals(other._comparer));
}
