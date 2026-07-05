// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a growable set of non-negative integers stored as a packed bit array, following the semantics of the Java
/// <c>BitSet</c> class: writes beyond the current capacity grow the storage automatically, and reads beyond the current
/// capacity report the bit as clear rather than throwing.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BitSet" /> is backed by a <see cref="ulong" /> array packing 64 bits per word. Single-bit
/// <see cref="Get(int)" />, <see cref="Set(int)" />, <see cref="Clear(int)" />, and <see cref="Flip(int)" /> operations
/// are O(1) (amortized when growth occurs); range operations and the word-wise logical operators (<see cref="And" />,
/// <see cref="Or" />, <see cref="Xor" />, <see cref="AndNot" />) process 64 bits per step.
/// </para>
/// <para>
/// The type distinguishes the <em>logical length</em> from the <em>allocated capacity</em>: <see cref="Length" /> is
/// one greater than the index of the highest set bit (the Java <c>length()</c> contract), while <see cref="Capacity" />
/// is the number of bits the current backing storage can address without reallocating. Reading a bit at or beyond
/// <see cref="Capacity" /> returns <see langword="false" />; clearing such a bit is a no-op; setting or flipping it
/// grows the storage.
/// </para>
/// <para>
/// The addressable universe is bounded: bit indices range from 0 to <see cref="MaxBitCount" /> − 1, which keeps
/// <see cref="Capacity" /> and <see cref="Length" /> representable as <see cref="int" /> values.
/// <see cref="Get(int)" /> and <see cref="Clear(int)" /> accept any non-negative index (indices beyond the universe are
/// trivially clear), whereas <see cref="Set(int)" /> and <see cref="Flip(int)" /> reject indices outside the universe.
/// </para>
/// <para>
/// Enumerating a <see cref="BitSet" /> yields the indices of the set bits in ascending order through a non-boxing
/// <see cref="Enumerator" /> struct. Any mutation invalidates active enumerators.
/// </para>
/// <para>
/// Equality is a value comparison over the logical content: two instances are equal when they have the same set bits,
/// regardless of allocated capacity. Because the content is mutable, the hash code changes as bits change — do not use
/// a <see cref="BitSet" /> as a dictionary key while it is being mutated.
/// </para>
/// <para>
/// Unlike <see cref="System.Collections.BitArray" /> — which has a fixed, explicit length, no set-bit query surface,
/// and a boxing enumerator over <see cref="bool" /> values — <see cref="BitSet" /> grows on demand, exposes
/// <see cref="NextSetBit" /> / <see cref="NextClearBit" /> / <see cref="Cardinality" />, and enumerates set-bit indices
/// without boxing.
/// </para>
/// <para>
/// <see cref="BitSet" /> is not thread-safe. Concurrent reads and writes require external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var primes = new BitSet();
/// primes.Set(2);
/// primes.Set(3);
/// primes.Set(5);
/// primes.Set(100);          // grows automatically
///
/// Console.WriteLine(primes.Cardinality);    // 4
/// Console.WriteLine(primes.Length);         // 101 — highest set bit + 1
/// Console.WriteLine(primes.Get(1_000_000)); // False — beyond capacity, no throw
///
/// foreach (int index in primes)
///     Console.WriteLine(index);             // 2, 3, 5, 100 — ascending
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Cardinality = {Cardinality}, Length = {Length}")]
[DebuggerTypeProxy(typeof(BitSetDebugView))]
public sealed partial class BitSet
    : IEnumerable<int>
{
    /// <summary>The number of addressable bits in the largest supported <see cref="BitSet" />. Bit indices range from 0 to <see cref="MaxBitCount" /> − 1.</summary>
    /// <remarks>
    /// The bound (<see cref="int.MaxValue" /> − 63) is the largest whole-word bit count whose capacity and logical
    /// length remain representable as <see cref="int" /> values.
    /// </remarks>
    public const int MaxBitCount = int.MaxValue - 63;

    /// <summary>The number of bits stored per backing word.</summary>
    private const int BitsPerWord = 64;

    /// <summary>The default capacity, in bits, of a <see cref="BitSet" /> created by the parameterless constructor.</summary>
    private const int DefaultCapacityBits = 64;

    /// <summary>The largest supported backing-array length, in words (<see cref="MaxBitCount" /> / 64).</summary>
    private const int MaxWordCount = MaxBitCount >> WordShift;

    /// <summary>The base-2 logarithm of <see cref="BitsPerWord" />, used to convert bit indices to word indices.</summary>
    private const int WordShift = 6;

    /// <summary>The backing bit storage, packing 64 bits per word; grown on demand, never <see langword="null" />.</summary>
    private ulong[] _words;

    /// <summary>Incremented on every mutation; used by enumerators to detect concurrent modification.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitSet" /> class with all bits clear and a small default capacity.
    /// </summary>
    public BitSet()
        : this(DefaultCapacityBits) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitSet" /> class with all bits clear and storage pre-allocated for
    /// at least the specified number of bits.
    /// </summary>
    /// <param name="initialCapacityBits">
    /// The number of bits the set can address before its first growth. A value of 0 allocates no storage.
    /// </param>
    /// <remarks>
    /// The requested capacity is rounded up to a whole number of 64-bit words, so <see cref="Capacity" /> may exceed
    /// <paramref name="initialCapacityBits" />. The capacity is a pre-allocation hint only — the set still grows
    /// automatically when a bit beyond it is set or flipped.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCapacityBits" /> &lt; 0, or <paramref name="initialCapacityBits" /> &gt;
    /// <see cref="MaxBitCount" />.
    /// </exception>
    public BitSet(int initialCapacityBits)
    {
        ThrowHelper.ThrowIfNegative(initialCapacityBits);
        ThrowHelper.ThrowIfGreaterThan(initialCapacityBits, MaxBitCount);

        _words = initialCapacityBits == 0
            ? []
            : new ulong[(int)(((long)initialCapacityBits + (BitsPerWord - 1)) >> WordShift)];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitSet" /> class containing the same set bits as the specified
    /// source set.
    /// </summary>
    /// <param name="source">The set whose bits are copied. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// The copy shares no storage with <paramref name="source" /> — subsequent mutations of either instance do not
    /// affect the other. The copy's <see cref="Capacity" /> matches the source's capacity at the time of the copy.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public BitSet(BitSet source)
    {
        ThrowHelper.ThrowIfNull(source);

        _words = [.. source._words];
    }

    /// <summary>
    /// Gets the number of bits the current backing storage can address without reallocating.
    /// </summary>
    /// <value>The allocated size in bits — always a multiple of 64.</value>
    /// <remarks>
    /// <para>
    /// <see cref="Capacity" /> is an allocation detail, distinct from the logical <see cref="Length" />: reading a bit
    /// at or beyond the capacity returns <see langword="false" />, clearing it is a no-op, and setting or flipping it
    /// grows the storage (increasing the capacity). The capacity never shrinks.
    /// </para>
    /// </remarks>
    public int Capacity =>
        _words.Length << WordShift;

    /// <summary>
    /// Gets the number of set bits in the set.
    /// </summary>
    /// <value>The population count — the total number of bits whose value is <see langword="true" />.</value>
    public int Cardinality
    {
        get
        {
            var count = 0;
            foreach (var word in _words)
                count += BitOperations.PopCount(word);

            return count;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the set contains no set bits.
    /// </summary>
    /// <value><see langword="true" /> if no bit is set; otherwise, <see langword="false" />.</value>
    public bool IsEmpty
    {
        get
        {
            foreach (var word in _words)
            {
                if (word != 0)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets the logical length of the set — one greater than the index of the highest set bit.
    /// </summary>
    /// <value>The highest set-bit index plus one, or 0 when the set is empty.</value>
    /// <remarks>
    /// <para>
    /// <see cref="Length" /> is the Java <c>BitSet.length()</c> contract and reflects only the logical content — it is
    /// unrelated to the allocated <see cref="Capacity" />. Clearing the highest set bit reduces the length; clearing
    /// every bit reduces it to 0 regardless of how much storage remains allocated.
    /// </para>
    /// </remarks>
    public int Length
    {
        get
        {
            for (var i = _words.Length - 1; i >= 0; i--)
            {
                if (_words[i] != 0)
                    return (i << WordShift) + (BitsPerWord - BitOperations.LeadingZeroCount(_words[i]));
            }

            return 0;
        }
    }

    /// <summary>
    /// Gets or sets the value of the bit at the specified index.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <value>
    /// <see langword="true" /> if the bit at <paramref name="index" /> is set; otherwise, <see langword="false" />.
    /// </value>
    /// <remarks>
    /// The getter follows the <see cref="Get(int)" /> contract (indices beyond <see cref="Capacity" /> read as
    /// <see langword="false" />); the setter follows the <see cref="Set(int, bool)" /> contract (auto-growing when a
    /// bit beyond the capacity is set to <see langword="true" />).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> &lt; 0, or — for the setter only — <paramref name="index" /> ≥
    /// <see cref="MaxBitCount" />.
    /// </exception>
    public bool this[int index]
    {
        get => Get(index);
        set => Set(index, value);
    }

    /// <summary>
    /// Clears every bit in the set.
    /// </summary>
    /// <remarks>
    /// The allocated <see cref="Capacity" /> is retained; only the logical content is reset. After this call
    /// <see cref="IsEmpty" /> is <see langword="true" /> and <see cref="Length" /> is 0.
    /// </remarks>
    public void Clear()
    {
        Array.Clear(_words);
        _version++;
    }

    /// <summary>
    /// Clears the bit at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the bit to clear.</param>
    /// <remarks>
    /// An index at or beyond <see cref="Capacity" /> is already conceptually clear, so the call is a no-op — it never
    /// grows the storage and never throws for large indices (Java <c>BitSet.clear</c> semantics).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> &lt; 0.</exception>
    public void Clear(int index)
    {
        ThrowHelper.ThrowIfNegative(index);

        var wordIndex = index >> WordShift;
        if (wordIndex < _words.Length)
            _words[wordIndex] &= ~(1UL << index);

        _version++;
    }

    /// <summary>
    /// Clears every bit in the half-open range [<paramref name="fromInclusive" />, <paramref name="toExclusive" />).
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    /// <remarks>
    /// An empty range (<paramref name="fromInclusive" /> equal to <paramref name="toExclusive" />) is a no-op. The
    /// portion of the range at or beyond <see cref="Capacity" /> is already conceptually clear, so the operation never
    /// grows the storage.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fromInclusive" /> &lt; 0, or <paramref name="toExclusive" /> &lt; 0.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fromInclusive" /> &gt; <paramref name="toExclusive" />.
    /// </exception>
    public void Clear(int fromInclusive, int toExclusive)
    {
        ThrowHelper.ThrowIfNegative(fromInclusive);
        ThrowHelper.ThrowIfNegative(toExclusive);
        ThrowHelper.ThrowIfGreaterThanOther(fromInclusive, toExclusive);

        // Clamp to the allocated storage; bits beyond it are already clear.
        var to = Math.Min(toExclusive, Capacity);
        if (fromInclusive < to)
            ClearRangeCore(fromInclusive, to);

        _version++;
    }

    /// <summary>
    /// Toggles the bit at the specified index — a set bit becomes clear and a clear bit becomes set.
    /// </summary>
    /// <param name="index">The zero-based index of the bit to toggle.</param>
    /// <remarks>
    /// Flipping a bit at or beyond <see cref="Capacity" /> sets it (the bit was conceptually clear), growing the
    /// storage as required.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> &lt; 0, or <paramref name="index" /> ≥ <see cref="MaxBitCount" />.
    /// </exception>
    public void Flip(int index)
    {
        ThrowHelper.ThrowIfNegative(index);
        ThrowHelper.ThrowIfGreaterThan(index, MaxBitCount - 1);

        EnsureBitCapacity(index);
        _words[index >> WordShift] ^= 1UL << index;
        _version++;
    }

    /// <summary>
    /// Toggles every bit in the half-open range [<paramref name="fromInclusive" />, <paramref name="toExclusive" />).
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    /// <remarks>
    /// An empty range (<paramref name="fromInclusive" /> equal to <paramref name="toExclusive" />) is a no-op. The
    /// storage grows as required so that clear bits beyond the current <see cref="Capacity" /> become set.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fromInclusive" /> &lt; 0, <paramref name="toExclusive" /> &lt; 0, or
    /// <paramref name="toExclusive" /> &gt; <see cref="MaxBitCount" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fromInclusive" /> &gt; <paramref name="toExclusive" />.
    /// </exception>
    public void Flip(int fromInclusive, int toExclusive)
    {
        ThrowHelper.ThrowIfNegative(fromInclusive);
        ThrowHelper.ThrowIfNegative(toExclusive);
        ThrowHelper.ThrowIfGreaterThanOther(fromInclusive, toExclusive);
        ThrowHelper.ThrowIfGreaterThan(toExclusive, MaxBitCount);

        if (fromInclusive < toExclusive)
        {
            EnsureBitCapacity(toExclusive - 1);
            FlipRangeCore(fromInclusive, toExclusive);
        }

        _version++;
    }

    /// <summary>
    /// Returns the value of the bit at the specified index.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <returns>
    /// <see langword="true" /> if the bit at <paramref name="index" /> is set; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// An index at or beyond <see cref="Capacity" /> returns <see langword="false" /> rather than throwing — every bit
    /// outside the allocated storage is conceptually clear (Java <c>BitSet.get</c> semantics). Any non-negative index,
    /// including <see cref="int.MaxValue" />, is accepted.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> &lt; 0.</exception>
    public bool Get(int index)
    {
        ThrowHelper.ThrowIfNegative(index);

        var wordIndex = index >> WordShift;
        return wordIndex < _words.Length && (_words[wordIndex] & (1UL << index)) != 0;
    }

    /// <summary>
    /// Sets the bit at the specified index to <see langword="true" />, growing the storage as required.
    /// </summary>
    /// <param name="index">The zero-based index of the bit to set.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> &lt; 0, or <paramref name="index" /> ≥ <see cref="MaxBitCount" />.
    /// </exception>
    public void Set(int index) =>
        Set(index, true);

    /// <summary>
    /// Sets the bit at the specified index to the specified value, growing the storage as required.
    /// </summary>
    /// <param name="index">The zero-based index of the bit to write.</param>
    /// <param name="value"><see langword="true" /> to set the bit; <see langword="false" /> to clear it.</param>
    /// <remarks>
    /// Storage grows only when a bit beyond the current <see cref="Capacity" /> is set to <see langword="true" />;
    /// writing <see langword="false" /> to such a bit is a no-op (it is already conceptually clear).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> &lt; 0, or <paramref name="index" /> ≥ <see cref="MaxBitCount" />.
    /// </exception>
    public void Set(int index, bool value)
    {
        ThrowHelper.ThrowIfNegative(index);
        ThrowHelper.ThrowIfGreaterThan(index, MaxBitCount - 1);

        if (value)
        {
            EnsureBitCapacity(index);
            _words[index >> WordShift] |= 1UL << index;
        }
        else
        {
            var wordIndex = index >> WordShift;
            if (wordIndex < _words.Length)
                _words[wordIndex] &= ~(1UL << index);
        }

        _version++;
    }

    /// <summary>
    /// Sets every bit in the half-open range [<paramref name="fromInclusive" />, <paramref name="toExclusive" />) to
    /// <see langword="true" />, growing the storage as required.
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    /// <remarks>
    /// An empty range (<paramref name="fromInclusive" /> equal to <paramref name="toExclusive" />) is a no-op.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fromInclusive" /> &lt; 0, <paramref name="toExclusive" /> &lt; 0, or
    /// <paramref name="toExclusive" /> &gt; <see cref="MaxBitCount" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fromInclusive" /> &gt; <paramref name="toExclusive" />.
    /// </exception>
    public void Set(int fromInclusive, int toExclusive)
    {
        ThrowHelper.ThrowIfNegative(fromInclusive);
        ThrowHelper.ThrowIfNegative(toExclusive);
        ThrowHelper.ThrowIfGreaterThanOther(fromInclusive, toExclusive);
        ThrowHelper.ThrowIfGreaterThan(toExclusive, MaxBitCount);

        if (fromInclusive < toExclusive)
        {
            EnsureBitCapacity(toExclusive - 1);
            SetRangeCore(fromInclusive, toExclusive);
        }

        _version++;
    }

    /// <summary>
    /// Returns the index of the first clear bit at or after the specified index.
    /// </summary>
    /// <param name="fromIndex">The index at which the search starts.</param>
    /// <returns>The index of the first clear bit ≥ <paramref name="fromIndex" />.</returns>
    /// <remarks>
    /// A clear bit conceptually always exists — every bit at or beyond <see cref="Capacity" /> is clear — so this
    /// method never returns −1. The result may therefore be ≥ <see cref="Length" /> (and ≥ <see cref="Capacity" /> when
    /// every allocated bit from <paramref name="fromIndex" /> onward is set).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fromIndex" /> &lt; 0.</exception>
    public int NextClearBit(int fromIndex)
    {
        ThrowHelper.ThrowIfNegative(fromIndex);

        var wordIndex = fromIndex >> WordShift;
        if (wordIndex >= _words.Length)
            return fromIndex;

        var word = ~_words[wordIndex] & (ulong.MaxValue << fromIndex);
        while (true)
        {
            if (word != 0)
                return (wordIndex << WordShift) + BitOperations.TrailingZeroCount(word);

            if (++wordIndex == _words.Length)
                return _words.Length << WordShift;

            word = ~_words[wordIndex];
        }
    }

    /// <summary>
    /// Returns the index of the first set bit at or after the specified index, or −1 when no further bit is set.
    /// </summary>
    /// <param name="fromIndex">The index at which the search starts.</param>
    /// <returns>The index of the first set bit ≥ <paramref name="fromIndex" />, or −1 if none exists.</returns>
    /// <remarks>
    /// The canonical iteration idiom is <c>for (int i = bits.NextSetBit(0); i >= 0; i = bits.NextSetBit(i + 1))</c>;
    /// the struct <see cref="Enumerator" /> obtained from <see cref="GetEnumerator" /> provides the same walk as a
    /// <see langword="foreach" /> loop.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fromIndex" /> &lt; 0.</exception>
    public int NextSetBit(int fromIndex)
    {
        ThrowHelper.ThrowIfNegative(fromIndex);

        return NextSetBitCore(fromIndex);
    }

    /// <summary>
    /// Returns a summary string describing the logical content of the set.
    /// </summary>
    /// <returns>
    /// A string of the form <c>BitSet(Cardinality = 5, Length = 65)</c> reporting <see cref="Cardinality" /> and
    /// <see cref="Length" />.
    /// </returns>
    /// <remarks>
    /// The summary form is deliberate — a set-builder listing could be arbitrarily large. Enumerate the instance to
    /// obtain the individual set-bit indices.
    /// </remarks>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"BitSet(Cardinality = {Cardinality}, Length = {Length})");

    /// <summary>
    /// Clears every bit in the non-empty half-open range [<paramref name="fromInclusive" />,
    /// <paramref name="toExclusive" />), which must lie within the allocated storage.
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    private void ClearRangeCore(int fromInclusive, int toExclusive)
    {
        var (firstWord, lastWord, firstMask, lastMask) = GetRangeMasks(fromInclusive, toExclusive);

        if (firstWord == lastWord)
        {
            _words[firstWord] &= ~(firstMask & lastMask);
            return;
        }

        _words[firstWord] &= ~firstMask;
        for (var i = firstWord + 1; i < lastWord; i++)
            _words[i] = 0;

        _words[lastWord] &= ~lastMask;
    }

    /// <summary>
    /// Toggles every bit in the non-empty half-open range [<paramref name="fromInclusive" />,
    /// <paramref name="toExclusive" />), which must lie within the allocated storage.
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    private void FlipRangeCore(int fromInclusive, int toExclusive)
    {
        var (firstWord, lastWord, firstMask, lastMask) = GetRangeMasks(fromInclusive, toExclusive);

        if (firstWord == lastWord)
        {
            _words[firstWord] ^= firstMask & lastMask;
            return;
        }

        _words[firstWord] ^= firstMask;
        for (var i = firstWord + 1; i < lastWord; i++)
            _words[i] ^= ulong.MaxValue;

        _words[lastWord] ^= lastMask;
    }

    /// <summary>
    /// Sets every bit in the non-empty half-open range [<paramref name="fromInclusive" />,
    /// <paramref name="toExclusive" />), which must lie within the allocated storage.
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    private void SetRangeCore(int fromInclusive, int toExclusive)
    {
        var (firstWord, lastWord, firstMask, lastMask) = GetRangeMasks(fromInclusive, toExclusive);

        if (firstWord == lastWord)
        {
            _words[firstWord] |= firstMask & lastMask;
            return;
        }

        _words[firstWord] |= firstMask;
        for (var i = firstWord + 1; i < lastWord; i++)
            _words[i] = ulong.MaxValue;

        _words[lastWord] |= lastMask;
    }

    /// <summary>
    /// Computes the word indices and edge masks for the non-empty half-open range [<paramref name="fromInclusive" />,
    /// <paramref name="toExclusive" />).
    /// </summary>
    /// <param name="fromInclusive">The first bit index of the range.</param>
    /// <param name="toExclusive">The exclusive upper bound of the range.</param>
    /// <returns>
    /// The first and last affected word indices and the partial-word masks for each; when the range spans a single
    /// word, the intersection of the two masks selects the affected bits.
    /// </returns>
    private static (int FirstWord, int LastWord, ulong FirstMask, ulong LastMask) GetRangeMasks(int fromInclusive, int toExclusive)
    {
        // Shift counts are masked to the low six bits by the C# shift operators, so shifting by
        // -toExclusive yields (64 - (toExclusive % 64)) % 64 — the standard trailing-mask idiom.
        return (
            fromInclusive >> WordShift,
            (toExclusive - 1) >> WordShift,
            ulong.MaxValue << fromInclusive,
            ulong.MaxValue >> -toExclusive);
    }

    /// <summary>
    /// Grows the backing storage, if necessary, so that the specified bit index is addressable.
    /// </summary>
    /// <param name="bitIndex">
    /// The highest bit index that must be addressable. Must be &lt; <see cref="MaxBitCount" />.
    /// </param>
    /// <remarks>
    /// Growth at least doubles the current word count (bounded by <see cref="MaxWordCount" />) so that repeated
    /// single-bit appends remain amortized O(1). Word arithmetic is performed in <see cref="long" /> to stay
    /// overflow-safe near <see cref="int.MaxValue" />.
    /// </remarks>
    private void EnsureBitCapacity(int bitIndex)
    {
        var requiredWords = (int)(((long)bitIndex + BitsPerWord) >> WordShift);
        if (requiredWords <= _words.Length)
            return;

        var newWordCount = Math.Min(MaxWordCount, Math.Max(requiredWords, _words.Length * 2));
        Array.Resize(ref _words, newWordCount);
    }

    /// <summary>
    /// Returns the index of the first set bit at or after the specified non-negative index, or −1 when no further bit
    /// is set. Performs no argument validation.
    /// </summary>
    /// <param name="fromIndex">The non-negative index at which the search starts.</param>
    /// <returns>The index of the first set bit ≥ <paramref name="fromIndex" />, or −1 if none exists.</returns>
    private int NextSetBitCore(int fromIndex)
    {
        var wordIndex = fromIndex >> WordShift;
        if (wordIndex >= _words.Length)
            return -1;

        var word = _words[wordIndex] & (ulong.MaxValue << fromIndex);
        while (true)
        {
            if (word != 0)
                return (wordIndex << WordShift) + BitOperations.TrailingZeroCount(word);

            if (++wordIndex == _words.Length)
                return -1;

            word = _words[wordIndex];
        }
    }

    /// <summary>
    /// Returns the number of backing words in use — the index of the highest non-zero word plus one.
    /// </summary>
    /// <returns>The count of words up to and including the highest non-zero word, or 0 when the set is empty.</returns>
    private int GetWordsInUse()
    {
        for (var i = _words.Length - 1; i >= 0; i--)
        {
            if (_words[i] != 0)
                return i + 1;
        }

        return 0;
    }
}
