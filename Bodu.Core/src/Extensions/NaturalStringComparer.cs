// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

/// <summary>
/// Provides a numeric-aware ("natural") string comparer that orders embedded runs of ASCII digits by numeric value
/// rather than character by character, so that <c>"file2"</c> sorts before <c>"file10"</c>.
/// </summary>
/// <remarks>
/// <para>
/// The comparer mirrors the ordering produced by Windows Explorer (<c>StrCmpLogicalW</c>) and Python's <c>natsort</c>
/// package: each string is treated as an alternating sequence of digit runs and non-digit segments. Digit runs are
/// compared by numeric magnitude — leading zeros are skipped, a longer trimmed run is greater, and equal-length trimmed
/// runs are compared digit by digit — so runs of arbitrary length compare correctly without ever being parsed into a
/// bounded integer type. Non-digit segments are compared ordinally ( <see cref="Ordinal" /> /
/// <see cref="OrdinalIgnoreCase" />) or using a culture's <see cref="CompareInfo" /> (<see cref="CurrentCulture" /> /
/// <see cref="CurrentCultureIgnoreCase" /> / <see cref="Create(CultureInfo, bool)" />).
/// </para>
/// <para>
/// To keep the order total and deterministic, two digit runs with the same numeric value but different leading-zero
/// counts do not compare equal: the first such difference is recorded and, when the strings are otherwise equal, the
/// run with fewer leading zeros sorts first (<c>"7"</c> before <c>"07"</c>). A digit run compared against a non-digit
/// character at the same position sorts first (numbers sort before text), and a string that ends where the other
/// continues sorts first (<c>"file"</c> before <c>"file1"</c>). <see langword="null" /> sorts before any non-<see langword="null" />
/// string, two <see langword="null" /> references are equal, and the empty string sorts before any non-empty string.
/// </para>
/// <para>
/// Only the ASCII digits <c>'0'</c>–<c>'9'</c> participate in numeric comparison; other Unicode digit classes are
/// treated as ordinary text. A leading <c>'-'</c> is an ordinary character (no sign parsing), and decimal points,
/// thousands separators, and version-sort dotted-tuple semantics are likewise out of scope — <c>'.'</c> simply
/// separates adjacent digit runs.
/// </para>
/// <para>
/// Instances are stateless and thread-safe. The <see cref="CurrentCulture" /> and
/// <see cref="CurrentCultureIgnoreCase" /> comparers capture the comparison behaviour, not a culture snapshot: each
/// comparison reads <see cref="CultureInfo.CurrentCulture" /> at the time it executes, mirroring
/// <see cref="StringComparer.CurrentCulture" />. A comparer returned by <see cref="Create(CultureInfo, bool)" />
/// captures the supplied culture instead.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var names = new[] { "file10", "file2", "file1" };
///
/// Array.Sort(names, NaturalStringComparer.Ordinal);
/// // names is now ["file1", "file2", "file10"] — a plain ordinal sort would yield
/// // ["file1", "file10", "file2"].
///]]>
/// </code>
/// </example>
public sealed class NaturalStringComparer
    : IComparer<string?>,
    System.Collections.IComparer,
    IEqualityComparer<string?>
{
    /// <summary>Hash-stream marker mixed in ahead of every digit-run hash so digit runs and text segments occupy disjoint positions in the composed hash.</summary>
    private const int DigitRunHashSeed = 0x1F123BB5;

    /// <summary>Hash-stream marker mixed in ahead of every text-segment hash so digit runs and text segments occupy disjoint positions in the composed hash.</summary>
    private const int TextSegmentHashSeed = 0x2C2F1B4B;

    /// <summary>The captured culture for comparers created via <see cref="Create(CultureInfo, bool)" />, or <see langword="null" /> for the ordinal modes and the current-culture modes (which read <see cref="CultureInfo.CurrentCulture" /> at comparison time).</summary>
    private readonly CultureInfo? _culture;

    /// <summary>Indicates whether non-digit segments are compared using a culture's <see cref="CompareInfo" /> rather than ordinally.</summary>
    private readonly bool _cultureAware;

    /// <summary>Indicates whether case differences are ignored when comparing non-digit segments.</summary>
    private readonly bool _ignoreCase;

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalStringComparer" /> class with the specified comparison
    /// behaviour.
    /// </summary>
    /// <param name="culture">
    /// The captured culture, or <see langword="null" /> to use ordinal comparison or, when
    /// <paramref name="cultureAware" /> is <see langword="true" />, the culture current at comparison time.
    /// </param>
    /// <param name="cultureAware">
    /// <see langword="true" /> to compare non-digit segments using a culture's <see cref="CompareInfo" />;
    /// <see langword="false" /> to compare them ordinally.
    /// </param>
    /// <param name="ignoreCase">
    /// <see langword="true" /> to ignore case when comparing non-digit segments; otherwise <see langword="false" />.
    /// </param>
    private NaturalStringComparer(CultureInfo? culture, bool cultureAware, bool ignoreCase)
    {
        _culture = culture;
        _cultureAware = cultureAware;
        _ignoreCase = ignoreCase;
    }

    /// <summary>
    /// Gets a <see cref="NaturalStringComparer" /> that compares non-digit segments using ordinal (binary)
    /// case-sensitive comparison.
    /// </summary>
    /// <value>A stateless, thread-safe natural comparer with ordinal, case-sensitive text comparison.</value>
    public static NaturalStringComparer Ordinal { get; } =
        new NaturalStringComparer(culture: null, cultureAware: false, ignoreCase: false);

    /// <summary>
    /// Gets a <see cref="NaturalStringComparer" /> that compares non-digit segments using ordinal (binary) comparison,
    /// ignoring case.
    /// </summary>
    /// <value>A stateless, thread-safe natural comparer with ordinal, case-insensitive text comparison.</value>
    public static NaturalStringComparer OrdinalIgnoreCase { get; } =
        new NaturalStringComparer(culture: null, cultureAware: false, ignoreCase: true);

    /// <summary>
    /// Gets a <see cref="NaturalStringComparer" /> that compares non-digit segments using culture-sensitive,
    /// case-sensitive comparison rules read from <see cref="CultureInfo.CurrentCulture" /> at comparison time.
    /// </summary>
    /// <value>
    /// A natural comparer whose text comparison follows the culture current when each comparison executes.
    /// </value>
    /// <remarks>
    /// The returned comparer captures the behaviour, not a culture snapshot: like
    /// <see cref="StringComparer.CurrentCulture" />, each call reads <see cref="CultureInfo.CurrentCulture" /> when the
    /// comparison is performed. Use <see cref="Create(CultureInfo, bool)" /> to capture a specific culture.
    /// </remarks>
    public static NaturalStringComparer CurrentCulture { get; } =
        new NaturalStringComparer(culture: null, cultureAware: true, ignoreCase: false);

    /// <summary>
    /// Gets a <see cref="NaturalStringComparer" /> that compares non-digit segments using culture-sensitive comparison
    /// rules read from <see cref="CultureInfo.CurrentCulture" /> at comparison time, ignoring case.
    /// </summary>
    /// <value>
    /// A natural comparer whose case-insensitive text comparison follows the culture current when each comparison
    /// executes.
    /// </value>
    /// <remarks>
    /// The returned comparer captures the behaviour, not a culture snapshot: like
    /// <see cref="StringComparer.CurrentCultureIgnoreCase" />, each call reads
    /// <see cref="CultureInfo.CurrentCulture" /> when the comparison is performed. Use
    /// <see cref="Create(CultureInfo, bool)" /> to capture a specific culture.
    /// </remarks>
    public static NaturalStringComparer CurrentCultureIgnoreCase { get; } =
        new NaturalStringComparer(culture: null, cultureAware: true, ignoreCase: true);

    /// <summary>
    /// Creates a <see cref="NaturalStringComparer" /> that compares non-digit segments using the comparison rules of
    /// the specified culture.
    /// </summary>
    /// <param name="culture">
    /// The culture whose comparison rules apply to non-digit segments. Must not be <see langword="null" />.
    /// </param>
    /// <param name="ignoreCase">
    /// <see langword="true" /> to ignore case when comparing non-digit segments; otherwise <see langword="false" />.
    /// </param>
    /// <returns>A new comparer bound to the supplied culture.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="culture" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Unlike <see cref="CurrentCulture" /> and <see cref="CurrentCultureIgnoreCase" />, the returned comparer captures
    /// <paramref name="culture" /> and is unaffected by later changes to <see cref="CultureInfo.CurrentCulture" />.
    /// </remarks>
    public static NaturalStringComparer Create(CultureInfo culture, bool ignoreCase)
    {
        ThrowHelper.ThrowIfNull(culture);

        return new NaturalStringComparer(culture, cultureAware: true, ignoreCase);
    }

    /// <summary>
    /// Compares two strings using natural (numeric-aware) ordering and returns an indication of their relative order.
    /// </summary>
    /// <param name="x">The first string to compare.</param>
    /// <param name="y">The second string to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative order of <paramref name="x" /> and <paramref name="y" />: less than
    /// zero when <paramref name="x" /> precedes <paramref name="y" />, zero when they are equal under this comparer,
    /// and greater than zero when <paramref name="x" /> follows <paramref name="y" />.
    /// </returns>
    /// <remarks>
    /// <see langword="null" /> sorts before any non-<see langword="null" /> string and two <see langword="null" />
    /// references compare equal. See the class-level remarks for the digit-run comparison rules and the leading-zero
    /// tiebreak that keeps the order total.
    /// </remarks>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        var compareInfo = _cultureAware ? (_culture ?? CultureInfo.CurrentCulture).CompareInfo : null;
        var options = _ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        var comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        var i = 0;
        var j = 0;
        var tiebreak = 0;

        while (i < x.Length || j < y.Length)
        {
            var xDigit = i < x.Length && char.IsAsciiDigit(x[i]);
            var yDigit = j < y.Length && char.IsAsciiDigit(y[j]);

            if (xDigit && yDigit)
            {
                var runResult = CompareDigitRuns(x, ref i, y, ref j, ref tiebreak);
                if (runResult != 0)
                    return runResult;

                continue;
            }

            // A digit run against an exhausted operand: the shorter string is a strict prefix and sorts first,
            // regardless of any pending leading-zero tiebreak.
            if (yDigit && i >= x.Length)
                return -1;

            if (xDigit && j >= y.Length)
                return 1;

            // Extract the maximal non-digit segments (empty when the position sits on a digit, which encodes the
            // "numbers sort before text" rule) and compare them per the configured mode.
            var xEnd = i;
            while (xEnd < x.Length && !char.IsAsciiDigit(x[xEnd]))
                xEnd++;

            var yEnd = j;
            while (yEnd < y.Length && !char.IsAsciiDigit(y[yEnd]))
                yEnd++;

            var segmentResult = compareInfo is null
                ? x.AsSpan(i, xEnd - i).CompareTo(y.AsSpan(j, yEnd - j), comparison)
                : compareInfo.Compare(x, i, xEnd - i, y, j, yEnd - j, options);

            if (segmentResult != 0)
                return Math.Sign(segmentResult);

            i = xEnd;
            j = yEnd;
        }

        return tiebreak;
    }

    /// <summary>
    /// Determines whether two strings are equal under this comparer's natural ordering.
    /// </summary>
    /// <param name="x">The first string to compare.</param>
    /// <param name="y">The second string to compare.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="Compare(string?, string?)" /> returns zero for <paramref name="x" /> and
    /// <paramref name="y" />; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Because of the leading-zero tiebreak, two strings whose digit runs differ only in leading zeros (for example
    /// <c>"file07"</c> and <c>"file7"</c>) are <b>not</b> equal. Equality therefore requires every digit run to match
    /// character for character and every non-digit segment to be equal under the configured text comparison.
    /// </remarks>
    public bool Equals(string? x, string? y) =>
        Compare(x, y) == 0;

    /// <summary>
    /// Returns a hash code for the specified string that is consistent with <see cref="Equals(string?, string?)" />.
    /// </summary>
    /// <param name="obj">The string for which to compute a hash code. Must not be <see langword="null" />.</param>
    /// <returns>A hash code such that any two strings equal under this comparer produce the same value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="obj" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// For the ordinal modes, equality under this comparer coincides with ordinal (or ordinal case-insensitive) string
    /// equality, so the hash is the string's own ordinal hash. For culture-aware modes, the hash is composed from each
    /// digit run (hashed verbatim) and each non-digit segment's
    /// <see cref="CompareInfo.GetHashCode(ReadOnlySpan{char}, CompareOptions)" /> value; segments that hash like the
    /// empty string (fully ignorable text) are skipped so strings differing only by ignorable characters hash
    /// identically.
    /// </para>
    /// <para>
    /// The culture-aware composition trades hash distribution for correctness: distinct strings may collide more often
    /// than under an ordinal hash, but equal strings are always assigned equal hash codes.
    /// </para>
    /// </remarks>
    public int GetHashCode(string? obj)
    {
        ThrowHelper.ThrowIfNull(obj);

        if (!_cultureAware)
            return obj.GetHashCode(_ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        var compareInfo = (_culture ?? CultureInfo.CurrentCulture).CompareInfo;
        var options = _ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        var emptySegmentHash = compareInfo.GetHashCode(ReadOnlySpan<char>.Empty, options);

        var hash = default(HashCode);
        var index = 0;

        while (index < obj.Length)
        {
            var start = index;

            if (char.IsAsciiDigit(obj[index]))
            {
                while (index < obj.Length && char.IsAsciiDigit(obj[index]))
                    index++;

                // Equality requires digit runs to match verbatim, so the run is hashed ordinally as written.
                hash.Add(DigitRunHashSeed);
                hash.Add(string.GetHashCode(obj.AsSpan(start, index - start), StringComparison.Ordinal));
            }
            else
            {
                while (index < obj.Length && !char.IsAsciiDigit(obj[index]))
                    index++;

                var segmentHash = compareInfo.GetHashCode(obj.AsSpan(start, index - start), options);

                // Segments that hash like the empty string are skipped so that a string carrying an extra
                // fully-ignorable segment hashes the same as its equal counterpart without one.
                if (segmentHash != emptySegmentHash)
                {
                    hash.Add(TextSegmentHashSeed);
                    hash.Add(segmentHash);
                }
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares two objects using natural (numeric-aware) ordering and returns an indication of their relative order.
    /// </summary>
    /// <param name="x">The first object to compare. Must be a <see cref="string" /> or <see langword="null" />.</param>
    /// <param name="y">
    /// The second object to compare. Must be a <see cref="string" /> or <see langword="null" />.
    /// </param>
    /// <returns>
    /// A signed integer that indicates the relative order of <paramref name="x" /> and <paramref name="y" />, as
    /// defined by <see cref="Compare(string?, string?)" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="x" /> or <paramref name="y" /> is neither a <see cref="string" /> nor
    /// <see langword="null" />.
    /// </exception>
    int System.Collections.IComparer.Compare(object? x, object? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        if (x is string left && y is string right)
            return Compare(left, right);

        throw new ArgumentException(
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MustBeOfType, nameof(String)),
            x is string ? nameof(y) : nameof(x));
    }

    /// <summary>
    /// Compares the digit runs starting at the current positions of both strings and advances each position past its
    /// run.
    /// </summary>
    /// <param name="x">The first string; <paramref name="i" /> must index an ASCII digit within it.</param>
    /// <param name="i">The current position in <paramref name="x" />; advanced past the digit run on return.</param>
    /// <param name="y">The second string; <paramref name="j" /> must index an ASCII digit within it.</param>
    /// <param name="j">The current position in <paramref name="y" />; advanced past the digit run on return.</param>
    /// <param name="tiebreak">
    /// The pending leading-zero tiebreak; when the runs have equal numeric value and the tiebreak is still unset, it
    /// records which run carries fewer leading zeros (that run sorts first if the strings are otherwise equal).
    /// </param>
    /// <returns>
    /// A signed integer indicating the relative numeric order of the two runs, or zero when they have equal numeric
    /// value.
    /// </returns>
    private static int CompareDigitRuns(string x, ref int i, string y, ref int j, ref int tiebreak)
    {
        var xStart = i;
        var yStart = j;

        while (i < x.Length && char.IsAsciiDigit(x[i]))
            i++;

        while (j < y.Length && char.IsAsciiDigit(y[j]))
            j++;

        // Skip leading zeros; an all-zero run trims to length zero and represents the value zero.
        var xs = xStart;
        while (xs < i && x[xs] == '0')
            xs++;

        var ys = yStart;
        while (ys < j && y[ys] == '0')
            ys++;

        var xLength = i - xs;
        var yLength = j - ys;

        // A longer trimmed run is a larger number; equal-length runs are decided by the first differing digit.
        if (xLength != yLength)
            return xLength < yLength ? -1 : 1;

        for (var k = 0; k < xLength; k++)
        {
            var cx = x[xs + k];
            var cy = y[ys + k];

            if (cx != cy)
                return cx < cy ? -1 : 1;
        }

        // Equal numeric value: record the first leading-zero difference so the overall order stays total
        // (fewer leading zeros sorts first when the strings are otherwise equal).
        if (tiebreak == 0)
            tiebreak = (xs - xStart).CompareTo(ys - yStart);

        return 0;
    }
}
