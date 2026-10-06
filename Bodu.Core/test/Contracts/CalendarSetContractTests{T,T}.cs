// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Contracts;

/// <summary>
/// Provides the behavioural contract shared by the calendar value sets, each a set of the values of a small, fixed
/// domain held one bit per value.
/// </summary>
/// <typeparam name="TSet">The set type under test.</typeparam>
/// <typeparam name="TElement">The type of the values the set selects.</typeparam>
/// <remarks>
/// <para>
/// A derived class describes its type's domain through <see cref="DomainSize" /> and <see cref="ElementAt(int)" />,
/// the value that bit <c>n</c> of the set selects, and reaches the members no interface declares through the abstract
/// adapters. The contract covers construction, <c>Empty</c>, <c>All</c>, <c>Count</c>, <c>Contains</c>, <c>With</c>,
/// <c>Without</c>, the bits round trip, the operators, equality and hashing, enumeration, and the storage every Bodu
/// calendar value set shares: one canonical bitmap in a <see cref="ulong" />, with the bits outside the domain clear.
/// </para>
/// <para>
/// The tests compare each answer with the expected membership of every value in the domain rather than with the set's
/// own bits, so a wrong bit order or a missing mask fails.
/// </para>
/// </remarks>
public abstract partial class CalendarSetContractTests<TSet, TElement>
    where TSet : struct, IEquatable<TSet>, IEqualityOperators<TSet, TSet, bool>, IBitwiseOperators<TSet, TSet, TSet>, IParsable<TSet>, IEnumerable<TElement>
{
    /// <summary>The seed of the random sets in <see cref="SampleSets" />, so that a failure reproduces.</summary>
    private const int SampleSeed = 20261005;

    /// <summary>The number of random sets <see cref="SampleSets" /> adds to the edge cases.</summary>
    private const int RandomSampleCount = 64;

    /// <summary>
    /// Gets the number of values in the domain.
    /// </summary>
    protected abstract int DomainSize { get; }

    /// <summary>
    /// Gets the type's set that selects no value.
    /// </summary>
    protected abstract TSet Empty { get; }

    /// <summary>
    /// Gets the type's set that selects every value in the domain.
    /// </summary>
    protected abstract TSet All { get; }

    /// <summary>
    /// Gets values outside the domain, which <c>Contains</c> must reject without throwing and the constructor,
    /// <c>With</c> and <c>Without</c> must reject by throwing.
    /// </summary>
    protected abstract IReadOnlyList<TElement> ElementsOutsideDomain { get; }

    /// <summary>
    /// Gets the name of the constructor's <see langword="params" /> parameter.
    /// </summary>
    protected abstract string ConstructorParameterName { get; }

    /// <summary>
    /// Gets the name of the value parameter of <c>With</c> and <c>Without</c>.
    /// </summary>
    protected abstract string ElementParameterName { get; }

    /// <summary>
    /// Gets the bits that select every value in the domain.
    /// </summary>
    protected ulong DomainBits =>
        DomainSize == 64 ? ulong.MaxValue : (1UL << DomainSize) - 1;

    /// <summary>
    /// Gets every value in the domain, in bit order.
    /// </summary>
    protected IEnumerable<TElement> Domain =>
        Enumerable.Range(0, DomainSize).Select(ElementAt);

    /// <summary>
    /// Returns the value that bit <paramref name="index" /> of a set selects.
    /// </summary>
    /// <param name="index">The bit index, from zero to one less than <see cref="DomainSize" />.</param>
    /// <returns>The value at <paramref name="index" />.</returns>
    protected abstract TElement ElementAt(int index);

    /// <summary>
    /// Creates a set through the type's <see langword="params" /> constructor.
    /// </summary>
    /// <param name="values">The values to select, or <see langword="null" />.</param>
    /// <returns>The new set.</returns>
    protected abstract TSet Create(params TElement[]? values);

    /// <summary>
    /// Creates a set from its bits through the type's <c>FromUInt64</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set the bits describe.</returns>
    protected abstract TSet FromUInt64(ulong bits);

    /// <summary>
    /// Returns a set's bits through the type's <c>ToUInt64</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>The set's bits.</returns>
    protected abstract ulong ToUInt64(TSet set);

    /// <summary>
    /// Returns a set's <c>Count</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>The number of values the set selects.</returns>
    protected abstract int Count(TSet set);

    /// <summary>
    /// Calls a set's <c>Contains</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value to test.</param>
    /// <returns>The set's answer.</returns>
    protected abstract bool Contains(TSet set, TElement value);

    /// <summary>
    /// Calls a set's <c>With</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The set's answer.</returns>
    protected abstract TSet With(TSet set, TElement value);

    /// <summary>
    /// Calls a set's <c>Without</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns>The set's answer.</returns>
    protected abstract TSet Without(TSet set, TElement value);

    /// <summary>
    /// Returns the sets the tests run over: the edge cases, then random sets from a fixed seed.
    /// </summary>
    /// <returns>The sample sets.</returns>
    protected IEnumerable<TSet> SampleSets()
    {
        int half = DomainSize / 2;

        yield return Empty;
        yield return All;
        yield return FromUInt64(1);
        yield return FromUInt64(1UL << (DomainSize - 1));
        yield return FromUInt64(1 | (1UL << (DomainSize - 1)));
        yield return FromUInt64(0x5555_5555_5555_5555UL & DomainBits);
        yield return FromUInt64(0xAAAA_AAAA_AAAA_AAAAUL & DomainBits);
        yield return FromUInt64(DomainBits >> half);
        yield return FromUInt64(DomainBits & ~(DomainBits >> half));

        var random = new Random(SampleSeed);
        for (int i = 0; i < RandomSampleCount; i++)
            yield return FromUInt64((ulong)random.NextInt64() & DomainBits);
    }

    /// <summary>
    /// Returns pairs of sample sets for the binary operators: each sample with itself, with the next, and with one
    /// further on.
    /// </summary>
    /// <returns>The sample pairs.</returns>
    protected IEnumerable<(TSet Left, TSet Right)> SamplePairs()
    {
        TSet[] samples = SampleSets().ToArray();
        int[] offsets = [0, 1, 7];

        for (int i = 0; i < samples.Length; i++)
        {
            foreach (int offset in offsets)
                yield return (samples[i], samples[(i + offset) % samples.Length]);
        }
    }

    /// <summary>
    /// Returns which values of the domain a set selects, according to its <c>Contains</c>.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>One flag per value, in bit order.</returns>
    protected bool[] Membership(TSet set) =>
        Domain.Select(value => Contains(set, value)).ToArray();

    /// <summary>
    /// Returns the membership a predicate over bit indexes describes.
    /// </summary>
    /// <param name="selects">The predicate that says whether the value at an index is selected.</param>
    /// <returns>One flag per value, in bit order.</returns>
    protected bool[] Expected(Func<int, bool> selects) =>
        Enumerable.Range(0, DomainSize).Select(selects).ToArray();

    /// <summary>
    /// Asserts that a set selects exactly the values a predicate over bit indexes describes.
    /// </summary>
    /// <param name="selects">The predicate that says whether the value at an index is selected.</param>
    /// <param name="actual">The set to check.</param>
    /// <param name="context">A description of the case, for the failure message.</param>
    protected void AssertSelects(Func<int, bool> selects, TSet actual, string context) =>
        CollectionAssert.AreEqual(Expected(selects), Membership(actual), context);
}
