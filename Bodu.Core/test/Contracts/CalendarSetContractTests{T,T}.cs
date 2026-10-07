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
/// the value that bit <c>n</c> of the set selects, and reaches the constructor through <see cref="Create" />. Every
/// other member is called through <see cref="ICalendarValueSet{TSelf, TValue}" />, so the contract also holds the
/// interface to each type. The contract covers construction, <c>Empty</c>, <c>All</c>, <c>Count</c>, <c>Contains</c>, <c>With</c>,
/// <c>Without</c>, the bits round trip, the operators, equality and hashing, enumeration, and the storage every Bodu
/// calendar value set shares: one canonical bitmap in a <see cref="ulong" />, with the bits outside the domain clear.
/// </para>
/// <para>
/// The tests compare each answer with the expected membership of every value in the domain rather than with the set's
/// own bits, so a wrong bit order or a missing mask fails.
/// </para>
/// </remarks>
public abstract partial class CalendarSetContractTests<TSet, TElement>
    where TSet : struct, ICalendarValueSet<TSet, TElement>
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
    /// Gets the type's set that selects no value, through <see cref="ICalendarValueSet{TSelf, TValue}.Empty" />.
    /// </summary>
    protected static TSet Empty =>
        TSet.Empty;

    /// <summary>
    /// Gets the type's set that selects every value in the domain, through
    /// <see cref="ICalendarValueSet{TSelf, TValue}.All" />.
    /// </summary>
    protected static TSet All =>
        TSet.All;

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
    /// Gets every format the type's <c>ToString(string)</c> accepts, in the cases it accepts them.
    /// </summary>
    protected abstract IReadOnlyList<string> Formats { get; }

    /// <summary>
    /// Gets formats that none of the six calendar value sets accepts.
    /// </summary>
    protected static IReadOnlyList<string> UnsupportedFormats { get; } =
        ["X", "Q", "GG", "G ", " G", "10", "011", "LG", "BB", "JJ"];

    /// <summary>
    /// Gets the empty format, which selects the default form, followed by every format in <see cref="Formats" />.
    /// </summary>
    protected IEnumerable<string> FormatsAndDefault =>
        Formats.Prepend(string.Empty);

    /// <summary>
    /// Returns the text the span and UTF-8 parsers are held to the string parsers over: every sample set in every
    /// format, and text that is not a set of any type.
    /// </summary>
    /// <returns>The inputs, valid and invalid.</returns>
    protected virtual IEnumerable<string> ParseInputs()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in FormatsAndDefault)
                yield return set.ToString(format, null);
        }

        string[] other =
        [
            string.Empty, " ", "x", ",", "-", "*", "0", "1", "01", "10", "\u00e9", "\u0661", "\u00a01\u00a0",
            new string('0', DomainSize), new string('1', DomainSize), new string('1', DomainSize + 1),
            "SMTWTFS", "smtwtfs", "_MTWTF_", "MF", "JFMAMJJASOND", "JFM________D", "jfm--------d",
        ];

        foreach (string text in other)
            yield return text;

        // Long enough that the UTF-8 parsers decode into a rented buffer rather than the stack.
        yield return new string(' ', 10_000) + All.ToString();
    }

    /// <summary>
    /// Calls the type's <see cref="IParsable{TSelf}.Parse(string, IFormatProvider)" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed set.</returns>
    /// <remarks>
    /// A call on <typeparamref name="TSet" /> itself with a string binds to <see cref="ISpanParsable{TSelf}" />'s
    /// overload instead, because that interface derives from <see cref="IParsable{TSelf}" /> and C# drops a base
    /// interface's members from the candidates; this helper's constraint names <see cref="IParsable{TSelf}" /> alone.
    /// </remarks>
    protected static TSet ParseString(string s, IFormatProvider? provider) =>
        ParseThroughIParsable<TSet>(s, provider);

    /// <summary>
    /// Calls the type's <see cref="IParsable{TSelf}.TryParse(string, IFormatProvider, out TSelf)" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">The parsed set, or the default when parsing fails.</param>
    /// <returns>The type's answer.</returns>
    /// <remarks>
    /// The call goes through <see cref="IParsable{TSelf}" /> for the reason <see cref="ParseString" /> gives.
    /// </remarks>
    protected static bool TryParseString(string? s, IFormatProvider? provider, out TSet result) =>
        TryParseThroughIParsable(s, provider, out result);

    /// <summary>
    /// Returns a description of a parse input for a failure message, with the control and non-ASCII characters
    /// escaped and long inputs shortened.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The description.</returns>
    protected static string Describe(string input)
    {
        string escaped = string.Concat(input.Select(c => c is >= ' ' and <= '~' ? c.ToString() : $"\\u{(int)c:x4}"));
        return escaped.Length <= 80 ? $"'{escaped}'" : $"'{escaped[..80]}...' ({input.Length} characters)";
    }

    /// <summary>
    /// Calls <see cref="IParsable{TSelf}.Parse(string, IFormatProvider)" /> on a type known only to implement
    /// <see cref="IParsable{TSelf}" />.
    /// </summary>
    /// <typeparam name="T">The type to parse.</typeparam>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed value.</returns>
    private static T ParseThroughIParsable<T>(string s, IFormatProvider? provider)
        where T : IParsable<T> =>
        T.Parse(s, provider);

    /// <summary>
    /// Calls <see cref="IParsable{TSelf}.TryParse(string, IFormatProvider, out TSelf)" /> on a type known only to
    /// implement <see cref="IParsable{TSelf}" />.
    /// </summary>
    /// <typeparam name="T">The type to parse.</typeparam>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">The parsed value, or the default when parsing fails.</param>
    /// <returns>The type's answer.</returns>
    private static bool TryParseThroughIParsable<T>(string? s, IFormatProvider? provider, out T result)
        where T : IParsable<T> =>
        T.TryParse(s, provider, out result!);

    /// <summary>
    /// Creates a set through the type's <see langword="params" /> constructor.
    /// </summary>
    /// <param name="values">The values to select, or <see langword="null" />.</param>
    /// <returns>The new set.</returns>
    protected abstract TSet Create(params TElement[]? values);

    /// <summary>
    /// Creates a set from its bits through <see cref="ICalendarValueSet{TSelf, TValue}.FromUInt64(ulong)" />.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set the bits describe.</returns>
    protected static TSet FromUInt64(ulong bits) =>
        TSet.FromUInt64(bits);

    /// <summary>
    /// Returns a set's bits through <see cref="ICalendarValueSet{TSelf, TValue}.ToUInt64" />.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>The set's bits.</returns>
    protected static ulong ToUInt64(TSet set) =>
        set.ToUInt64();

    /// <summary>
    /// Returns a set's <see cref="ICalendarValueSet{TSelf, TValue}.Count" />.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>The number of values the set selects.</returns>
    protected static int Count(TSet set) =>
        set.Count;

    /// <summary>
    /// Calls a set's <see cref="ICalendarValueSet{TSelf, TValue}.Contains(TValue)" />.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value to test.</param>
    /// <returns>The set's answer.</returns>
    protected static bool Contains(TSet set, TElement value) =>
        set.Contains(value);

    /// <summary>
    /// Calls a set's <see cref="ICalendarValueSet{TSelf, TValue}.With(TValue)" />.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The set's answer.</returns>
    protected static TSet With(TSet set, TElement value) =>
        set.With(value);

    /// <summary>
    /// Calls a set's <see cref="ICalendarValueSet{TSelf, TValue}.Without(TValue)" />.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns>The set's answer.</returns>
    protected static TSet Without(TSet set, TElement value) =>
        set.Without(value);

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
