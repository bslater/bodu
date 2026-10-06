// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

/// <summary>
/// Provides the behavioural contract of the numeric calendar value sets, whose values are the integers from a minimum
/// to a maximum and whose text form is a comma-separated list of values and inclusive ranges.
/// </summary>
/// <typeparam name="TSet">The set type under test.</typeparam>
/// <remarks>
/// <para>
/// The domain is described by <see cref="Minimum" /> and <see cref="Maximum" />; bit <c>n</c> selects
/// <c>Minimum + n</c>. On top of <see cref="CalendarSetContractTests{TSet, TElement}" />, the contract covers
/// <c>Parse</c>, <c>TryParse</c>, the explicit <see cref="IParsable{TSelf}" /> members and <c>ToString</c>.
/// </para>
/// <para>
/// The text rows are built from the domain's bounds, so every type runs the same cases at its own edges; a derived
/// class adds canonical rows of its own through <see cref="CanonicalTextCases" />.
/// </para>
/// </remarks>
public abstract partial class CalendarValueSetContractTests<TSet>
    : CalendarSetContractTests<TSet, int>
    where TSet : struct, ICalendarValueSet<TSet, int>
{
    /// <summary>
    /// Gets the smallest value in the domain.
    /// </summary>
    protected abstract int Minimum { get; }

    /// <summary>
    /// Gets the largest value in the domain.
    /// </summary>
    protected abstract int Maximum { get; }

    /// <summary>
    /// Gets the type's own canonical text rows: each input is the text <c>ToString</c> writes for the expected set.
    /// </summary>
    protected abstract IReadOnlyList<ValidKat<string, TSet>> CanonicalTextCases { get; }

    /// <inheritdoc />
    protected sealed override int DomainSize =>
        Maximum - Minimum + 1;

    /// <inheritdoc />
    /// <remarks>
    /// <c>Minimum + 64</c> is included because a shift by 64 bits wraps to a shift by none, so a set that skipped its
    /// range check would answer for <c>Minimum</c> instead.
    /// </remarks>
    protected sealed override IReadOnlyList<int> ElementsOutsideDomain =>
        [Minimum - 1, Maximum + 1, Minimum + 64, int.MinValue, int.MaxValue];

    /// <inheritdoc />
    protected sealed override int ElementAt(int index) =>
        Minimum + index;

    /// <summary>
    /// Calls the type's <c>Parse</c>.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <returns>The parsed set.</returns>
    protected abstract TSet Parse(string s);

    /// <summary>
    /// Calls the type's <c>TryParse</c>.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="result">The parsed set, or the default when parsing fails.</param>
    /// <returns>The type's answer.</returns>
    protected abstract bool TryParse(string? s, out TSet result);

    /// <summary>
    /// Calls the type's <see cref="ICalendarValueSet{TSelf, TValue}.ToString(string)" />.
    /// </summary>
    /// <param name="set">The set to write.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The text the type writes.</returns>
    protected static string Format(TSet set, string? format) =>
        set.ToString(format);

    /// <summary>
    /// Calls the type's <see cref="ICalendarValueSet{TSelf, TValue}.ParseExact(string, string)" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="format">The format the text must have.</param>
    /// <returns>The parsed set.</returns>
    protected static TSet ParseExact(string s, string format) =>
        TSet.ParseExact(s, format);

    /// <summary>
    /// Calls the type's <see cref="ICalendarValueSet{TSelf, TValue}.TryParseExact(string, string, out TSelf)" />.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="format">The format the text must have.</param>
    /// <param name="result">The parsed set, or the default when parsing fails.</param>
    /// <returns>The type's answer.</returns>
    protected static bool TryParseExact(string? s, string? format, out TSet result) =>
        TSet.TryParseExact(s, format, out result);

    /// <summary>
    /// Returns the canonical text rows built from the domain's bounds, followed by the type's own.
    /// </summary>
    /// <returns>The canonical rows.</returns>
    protected IEnumerable<ValidKat<string, TSet>> CanonicalCases()
    {
        int min = Minimum;
        int max = Maximum;

        yield return new("empty", string.Empty, Empty);
        yield return new("minimum", Invariant($"{min}"), Create(min));
        yield return new("maximum", Invariant($"{max}"), Create(max));
        yield return new("minimum and maximum", Invariant($"{min},{max}"), Create(min, max));
        yield return new("whole domain", Invariant($"{min}-{max}"), All);
        yield return new("pair at the top", Invariant($"{max - 1}-{max}"), Create(max - 1, max));
        yield return new("value, range and value", Invariant($"{min},{min + 2}-{min + 4},{max}"), Create(min, min + 2, min + 3, min + 4, max));

        foreach (ValidKat<string, TSet> kat in CanonicalTextCases)
            yield return kat;
    }

    /// <summary>
    /// Returns valid text rows that are not in canonical form: whitespace, order, repetition, overlap, adjacent values
    /// and leading zeros.
    /// </summary>
    /// <returns>The lenient rows.</returns>
    protected IEnumerable<ValidKat<string, TSet>> LenientCases()
    {
        int min = Minimum;
        int max = Maximum;

        yield return new("blank", "   ", Empty);
        yield return new("whitespace around the list and items", Invariant($" {min} , {max} "), Create(min, max));
        yield return new("whitespace around range bounds", Invariant($"{min} - {min + 2}"), Create(min, min + 1, min + 2));
        yield return new("tab and newline", Invariant($"\t{min}\n"), Create(min));
        yield return new("descending order", Invariant($"{max},{min}"), Create(min, max));
        yield return new("repeated value", Invariant($"{min},{min}"), Create(min));
        yield return new("overlapping ranges", Invariant($"{min}-{min + 3},{min + 2}-{min + 5}"), Create(Enumerable.Range(min, 6).ToArray()));
        yield return new("ranges that join", Invariant($"{min}-{min + 1},{min + 2}-{min + 3}"), Create(Enumerable.Range(min, 4).ToArray()));
        yield return new("single-value range", Invariant($"{min + 1}-{min + 1}"), Create(min + 1));
        yield return new("adjacent values", Invariant($"{min},{min + 1}"), Create(min, min + 1));
        yield return new("leading zeros", Invariant($"00{min + 1}"), Create(min + 1));
    }

    /// <summary>
    /// Returns text rows that are not a list of values and ranges of the domain.
    /// </summary>
    /// <returns>The malformed rows, each expecting <see cref="FormatException" />.</returns>
    protected IEnumerable<InvalidKat<string>> MalformedCases()
    {
        int min = Minimum;
        int max = Maximum;

        string[][] rows =
        [
            ["letter", "a"],
            ["trailing comma", Invariant($"{min},")],
            ["leading comma", Invariant($",{min}")],
            ["empty item", Invariant($"{min},,{max}")],
            ["comma only", ","],
            ["semicolon separator", Invariant($"{min};{max}")],
            ["minus sign", Invariant($"-{min + 1}")],
            ["plus sign", Invariant($"+{min}")],
            ["descending range", Invariant($"{min + 2}-{min}")],
            ["range without an end", Invariant($"{min}-")],
            ["dash only", "-"],
            ["three-part range", Invariant($"{min}-{min + 1}-{min + 2}")],
            ["space inside a value", Invariant($"{min} {min + 1}")],
            ["above the domain", Invariant($"{max + 1}")],
            ["range past the domain", Invariant($"{min}-{max + 1}")],
            ["beyond Int32", "99999999999"],
            ["decimal point", Invariant($"{min}.0")],
            ["hexadecimal", "0x1"],
            ["non-ASCII digit", "١"],
            ["step", Invariant($"{min}/2")],
            ["wildcard", "*"],
        ];

        foreach (string[] row in rows)
            yield return new(row[0], row[1], typeof(FormatException));

        // Below the domain only where the value is unsigned; a minimum of zero has nothing below it but "-1",
        // which the "minus sign" row already covers.
        if (min > 0)
            yield return new("below the domain", Invariant($"{min - 1}"), typeof(FormatException));
    }

    /// <summary>
    /// Returns text rows that pin the order in which <c>Parse</c> and <c>TryParse</c> detect a text's form: exactly one
    /// <c>0</c> or <c>1</c> per value of the domain, as given, is the binary form, even where the text also reads as a
    /// list, and any other text is a list.
    /// </summary>
    /// <returns>The detection rows.</returns>
    /// <remarks>
    /// The list rows read as the values 1, 10 and 11, which every numeric domain holds.
    /// </remarks>
    protected IEnumerable<ValidKat<string, TSet>> DetectionCases()
    {
        int width = DomainSize;
        string zeros = new('0', width - 1);

        // Binary first. As lists, the first two would read as the numbers 0 and 1, and the last two would overflow.
        yield return new("binary of no value", new string('0', width), Empty);
        yield return new("binary of the maximum", zeros + "1", Create(Maximum));
        yield return new("binary of the minimum", "1" + zeros, Create(Minimum));
        yield return new("binary of every value", new string('1', width), All);

        // Zeros and ones of any other length, or with whitespace, are a list.
        yield return new("one", "1", Create(1));
        yield return new("ten", "10", Create(10));
        yield return new("eleven", "11", Create(11));
        yield return new("one digit short of the binary form", zeros[1..] + "1", Create(1));
        yield return new("one digit past the binary form", zeros + "01", Create(1));
        yield return new("binary text after a space", " " + zeros + "1", Create(1));
        yield return new("binary text before a space", zeros + "1 ", Create(1));
        yield return new("the binary form's length, padded with spaces", "1" + new string(' ', width - 1), Create(1));
    }

    /// <summary>
    /// Returns text rows that are neither the binary form nor a list, though each has the binary form's length or
    /// holds binary text.
    /// </summary>
    /// <returns>The rows, each expecting <see cref="FormatException" />.</returns>
    protected IEnumerable<InvalidKat<string>> DetectionFailureCases()
    {
        int width = DomainSize;
        string zeros = new('0', width - 1);

        yield return new("the binary form's length, with a 2", "2" + zeros, typeof(FormatException));
        yield return new("the binary form's length, with a letter", zeros + "x", typeof(FormatException));
        yield return new("binary text of the minimum after a space", " 1" + zeros, typeof(FormatException));
    }

    /// <summary>
    /// Writes the canonical text of a set of values: ascending, comma-separated, with each run of two or more
    /// consecutive values as an inclusive range.
    /// </summary>
    /// <param name="values">The values, in any order and without repeats.</param>
    /// <returns>The canonical text.</returns>
    /// <remarks>
    /// This is a plain reading of the format, independent of the implementation, for the tests to compare with.
    /// </remarks>
    protected static string FormatReference(IEnumerable<int> values)
    {
        int[] sorted = values.Order().ToArray();
        var parts = new List<string>();

        int i = 0;
        while (i < sorted.Length)
        {
            int start = sorted[i];
            int end = start;
            while (i + 1 < sorted.Length && sorted[i + 1] == end + 1)
            {
                i++;
                end++;
            }

            parts.Add(start == end ? Invariant($"{start}") : Invariant($"{start}-{end}"));
            i++;
        }

        return string.Join(",", parts);
    }

    /// <summary>
    /// Writes every value a set's bits select, ascending and comma-separated, without ranges: the <c>L</c> format.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The list, or the empty string when no bit is set.</returns>
    /// <remarks>
    /// This is a plain reading of the format, independent of the implementation, for the tests to compare with.
    /// </remarks>
    protected string ValuesReference(ulong bits) =>
        string.Join(",", ValuesOf(bits).Select(value => Invariant($"{value}")));

    /// <summary>
    /// Writes one digit per value of the domain, lowest value first, <c>1</c> for a value the bits select and
    /// <c>0</c> for one they do not: the binary format.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The binary text, one character per value of the domain.</returns>
    /// <remarks>
    /// This is a plain reading of the format, independent of the implementation, for the tests to compare with.
    /// </remarks>
    protected string BinaryReference(ulong bits) =>
        new(Enumerable.Range(0, DomainSize).Select(i => ((bits >> i) & 1) != 0 ? '1' : '0').ToArray());

    /// <summary>
    /// Returns the values a set's bits select, read from the bits rather than from the set.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The values, in ascending order.</returns>
    protected IEnumerable<int> ValuesOf(ulong bits) =>
        Enumerable.Range(0, DomainSize).Where(i => ((bits >> i) & 1) != 0).Select(ElementAt);

    /// <summary>
    /// Formats an interpolated string with the invariant culture.
    /// </summary>
    /// <param name="text">The interpolated string.</param>
    /// <returns>The formatted text.</returns>
    private static string Invariant(FormattableString text) =>
        FormattableString.Invariant(text);
}
