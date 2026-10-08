// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICalendarValueSet{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Defines an immutable set of values drawn from a fixed calendar domain of at most 64 values, such as the days of the
/// week or the minutes of an hour.
/// </summary>
/// <typeparam name="TSelf">The type that implements the interface.</typeparam>
/// <typeparam name="TValue">The type of the values in the domain.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DayOfWeekSet" />, <see cref="MonthSet" />, <see cref="DayOfMonthSet" />, <see cref="HourSet" />,
/// <see cref="MinuteSet" /> and <see cref="SecondSet" /> implement it, so generic code can test, combine, write and
/// read any of them. A set is a value: members that change it, such as <see cref="With(TValue)" /> and the operators,
/// return a new set, and two sets are equal when they select the same values.
/// </para>
/// <para>
/// The operators are the set operations within the domain: <c>|</c> is the union, <c>&amp;</c> the intersection,
/// <c>^</c> the symmetric difference, and <c>~</c> the complement, every value of the domain the set does not select.
/// </para>
/// <para>
/// Each value of the domain has a position, from 0 for the first value to one less than the domain's size for the last.
/// <see cref="ToUInt64" /> describes a set as the number whose bit <c>n</c> is set when the set selects the value at
/// position <c>n</c>, and <see cref="FromUInt64(ulong)" /> creates the set such a number describes, so equal sets give
/// equal numbers.
/// </para>
/// <para>
/// Each type documents its domain, its text forms and the formats <see cref="ToString(string)" /> and
/// <see cref="ParseExact(string, string)" /> accept. Every text form is ASCII, so the members that write to or read
/// from a span of characters or of UTF-8 bytes handle the same text as the members that take and return a
/// <see cref="string" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// static string Describe<TSet, TValue>(TSet set)
///     where TSet : ICalendarValueSet<TSet, TValue> =>
///     set == TSet.All ? "all" : $"{set.Count} selected: {set}";
///
/// Describe<MonthSet, int>(new MonthSet(1, 4, 7, 10));       // "4 selected: 1,4,7,10"
/// Describe<DayOfWeekSet, DayOfWeek>(DayOfWeekSet.Weekdays); // "5 selected: _MTWTF_"
///]]>
/// </code>
/// </example>
public interface ICalendarValueSet<TSelf, TValue>
    : IEquatable<TSelf>,
      IEqualityOperators<TSelf, TSelf, bool>,
      IBitwiseOperators<TSelf, TSelf, TSelf>,
      IParsable<TSelf>,
      ISpanParsable<TSelf>,
      IUtf8SpanParsable<TSelf>,
      IFormattable,
      ISpanFormattable,
      IUtf8SpanFormattable,
      IEnumerable<TValue>
    where TSelf : ICalendarValueSet<TSelf, TValue>
{
    /// <summary>
    /// Gets the set that selects no value.
    /// </summary>
    static abstract TSelf Empty { get; }

    /// <summary>
    /// Gets the set that selects every value of the domain.
    /// </summary>
    static abstract TSelf All { get; }

    /// <summary>
    /// Gets the number of values the set selects.
    /// </summary>
    /// <value>A number from 0 to the size of the domain.</value>
    int Count { get; }

    /// <summary>
    /// Creates the set that a number describes, bit <c>n</c> selecting the value at position <c>n</c> of the domain.
    /// </summary>
    /// <param name="bits">The number that describes the set.</param>
    /// <returns>The set <paramref name="bits" /> describes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit at a position past the end of the domain, which selects no
    /// value.
    /// </exception>
    static abstract TSelf FromUInt64(ulong bits);

    /// <summary>
    /// Returns the number that describes the set, bit <c>n</c> set when the set selects the value at position <c>n</c>
    /// of the domain.
    /// </summary>
    /// <returns>The number, with no bit set at a position past the end of the domain.</returns>
    ulong ToUInt64();

    /// <summary>
    /// Determines whether the set selects the specified value.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="value" />; <see langword="false" /> when it does
    /// not, including when <paramref name="value" /> is outside the domain.
    /// </returns>
    bool Contains(TValue value);

    /// <summary>
    /// Returns a set that also selects the specified value.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>A set that selects <paramref name="value" /> and every value this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is outside the domain.
    /// </exception>
    TSelf With(TValue value);

    /// <summary>
    /// Returns a set that no longer selects the specified value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns>A set that selects every value this set selects except <paramref name="value" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is outside the domain.
    /// </exception>
    TSelf Without(TValue value);

    /// <summary>
    /// Converts text in the specified format into a set.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="format">The format of <paramref name="s" />, one of those the type writes.</param>
    /// <returns>The set the text describes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> or <paramref name="format" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported format, or <paramref name="s" /> is not text in that
    /// format.
    /// </exception>
    static abstract TSelf ParseExact(string s, string format);

    /// <summary>
    /// Attempts to convert text in the specified format into a set.
    /// </summary>
    /// <param name="s">The text to convert.</param>
    /// <param name="format">The format of <paramref name="s" />, one of those the type writes.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the text describes; otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format and <paramref name="s" /> is text
    /// in it; otherwise <see langword="false" />.
    /// </returns>
    static abstract bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)] string? format, out TSelf result);

    /// <summary>
    /// Converts a span of characters in the specified format into a set.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <param name="format">The format of <paramref name="s" />, one of those the type writes.</param>
    /// <returns>The set the characters describe.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported format, or <paramref name="s" /> is not text in that
    /// format.
    /// </exception>
    static abstract TSelf ParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format);

    /// <summary>
    /// Attempts to convert a span of characters in the specified format into a set.
    /// </summary>
    /// <param name="s">The characters to convert.</param>
    /// <param name="format">The format of <paramref name="s" />, one of those the type writes.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the set the characters describe; otherwise
    /// <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="format" /> is a supported format and <paramref name="s" /> is text
    /// in it; otherwise <see langword="false" />.
    /// </returns>
    static abstract bool TryParseExact(ReadOnlySpan<char> s, ReadOnlySpan<char> format, out TSelf result);

    /// <summary>
    /// Returns the set as text in the specified format.
    /// </summary>
    /// <param name="format">The format, or <see langword="null" /> or empty for the default.</param>
    /// <returns>The text.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format" /> is not a supported format.</exception>
    string ToString(string? format);
}
