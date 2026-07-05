// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSet.Operations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class BitSet
    : IEquatable<BitSet>
{
    /// <summary>
    /// Determines whether two <see cref="BitSet" /> instances contain the same set bits.
    /// </summary>
    /// <param name="left">The first operand, or <see langword="null" />.</param>
    /// <param name="right">The second operand, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if both operands are <see langword="null" /> or contain the same set bits; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator ==(BitSet? left, BitSet? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="BitSet" /> instances contain different set bits.
    /// </summary>
    /// <param name="left">The first operand, or <see langword="null" />.</param>
    /// <param name="right">The second operand, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the operands differ in at least one set bit (or exactly one is
    /// <see langword="null" />); otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(BitSet? left, BitSet? right) =>
        !(left == right);

    /// <summary>
    /// Performs an in-place logical AND with the specified set: a bit remains set only when it is set in both operands.
    /// </summary>
    /// <param name="other">The set to intersect with. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// The result can never contain bits beyond either operand's content, so this operation never grows the storage.
    /// Bits of this set beyond <paramref name="other" />'s capacity are cleared (they are ANDed with conceptually clear
    /// bits). Passing this instance as <paramref name="other" /> is a no-op.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void And(BitSet other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return;

        var common = Math.Min(_words.Length, other._words.Length);
        for (var i = 0; i < common; i++)
            _words[i] &= other._words[i];

        // Bits beyond the other operand's storage are conceptually clear in it, so they clear here.
        for (var i = common; i < _words.Length; i++)
            _words[i] = 0;

        _version++;
    }

    /// <summary>
    /// Performs an in-place logical AND NOT (set difference) with the specified set: every bit that is set in
    /// <paramref name="other" /> is cleared in this set.
    /// </summary>
    /// <param name="other">The set whose bits are removed from this set. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// The result is a subset of this set's content, so this operation never grows the storage. Passing this instance
    /// as <paramref name="other" /> clears every bit.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void AndNot(BitSet other)
    {
        ThrowHelper.ThrowIfNull(other);

        var common = Math.Min(_words.Length, other._words.Length);
        for (var i = 0; i < common; i++)
            _words[i] &= ~other._words[i];

        _version++;
    }

    /// <summary>
    /// Determines whether the specified object is a <see cref="BitSet" /> containing the same set bits as this
    /// instance.
    /// </summary>
    /// <param name="obj">The object to compare, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="BitSet" /> with the same set bits; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        Equals(obj as BitSet);

    /// <summary>
    /// Determines whether the specified <see cref="BitSet" /> contains the same set bits as this instance.
    /// </summary>
    /// <param name="other">The set to compare, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> has the same set bits; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Equality is a value comparison over the logical content only: allocated capacity and trailing zero words are
    /// ignored, so a set that grew and was subsequently cleared equals a freshly constructed empty set.
    /// </remarks>
    public bool Equals(BitSet? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(other, this))
            return true;

        var wordsInUse = GetWordsInUse();
        if (wordsInUse != other.GetWordsInUse())
            return false;

        for (var i = 0; i < wordsInUse; i++)
        {
            if (_words[i] != other._words[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a hash code derived from the logical content of the set.
    /// </summary>
    /// <returns>An <see cref="int" /> hash code consistent with <see cref="Equals(BitSet)" />.</returns>
    /// <remarks>
    /// The hash covers only the words up to the highest set bit, so trailing zero words (excess capacity) do not affect
    /// it. Because the content is mutable, the hash code changes as bits change.
    /// </remarks>
    public override int GetHashCode()
    {
        // Java BitSet.hashCode(): fold each in-use word, weighted by position, into a 64-bit
        // accumulator and mix the halves. Trailing zero words contribute nothing by construction.
        var h = 1234UL;
        var wordsInUse = GetWordsInUse();
        for (var i = 0; i < wordsInUse; i++)
            h ^= _words[i] * ((ulong)i + 1);

        return (int)((h >> 32) ^ h);
    }

    /// <summary>
    /// Determines whether this set and the specified set have at least one set bit in common.
    /// </summary>
    /// <param name="other">The set to test against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if any bit is set in both this set and <paramref name="other" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Neither operand is modified. Passing this instance as <paramref name="other" /> returns <see langword="true" />
    /// exactly when the set is non-empty.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool Intersects(BitSet other)
    {
        ThrowHelper.ThrowIfNull(other);

        var common = Math.Min(_words.Length, other._words.Length);
        for (var i = 0; i < common; i++)
        {
            if ((_words[i] & other._words[i]) != 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Performs an in-place logical OR with the specified set: a bit becomes set when it is set in either operand.
    /// </summary>
    /// <param name="other">The set to union with. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// The storage grows as required to accommodate <paramref name="other" />'s logical <see cref="Length" />, so bits
    /// set beyond this set's current capacity are preserved in the result. Passing this instance as
    /// <paramref name="other" /> is a no-op.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void Or(BitSet other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return;

        var otherWordsInUse = other.GetWordsInUse();
        if (otherWordsInUse > 0)
            EnsureBitCapacity((otherWordsInUse << WordShift) - 1);

        for (var i = 0; i < otherWordsInUse; i++)
            _words[i] |= other._words[i];

        _version++;
    }

    /// <summary>
    /// Performs an in-place logical XOR with the specified set: a bit becomes set when it is set in exactly one of the
    /// two operands (symmetric difference).
    /// </summary>
    /// <param name="other">The set to combine with. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// The storage grows as required to accommodate <paramref name="other" />'s logical <see cref="Length" />. Passing
    /// this instance as <paramref name="other" /> clears every bit.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void Xor(BitSet other)
    {
        ThrowHelper.ThrowIfNull(other);

        var otherWordsInUse = other.GetWordsInUse();
        if (otherWordsInUse > 0)
            EnsureBitCapacity((otherWordsInUse << WordShift) - 1);

        for (var i = 0; i < otherWordsInUse; i++)
            _words[i] ^= other._words[i];

        _version++;
    }
}
