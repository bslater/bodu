// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronLibraryVectorKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents one cron scenario taken from another cron library, either from its test suite or from a fix in its
/// release history, together with the outcome restated in Bodu's dialect.
/// </summary>
/// <param name="Name">The label that identifies the row in failure diagnostics.</param>
/// <param name="Kind">
/// The assertion the row makes: <c>next</c>, <c>next-inclusive</c>, <c>previous</c>, <c>previous-inclusive</c>,
/// <c>next-sequence</c>, <c>previous-sequence</c>, <c>unreachable-next</c>, <c>unreachable-previous</c>,
/// <c>invalid</c>, <c>valid</c>, <c>equal</c>, <c>not-equal</c>, or <c>to-string</c>.
/// </param>
/// <param name="Format">The field layout the expression is parsed with.</param>
/// <param name="Expression">The cron text under test, in Bodu's dialect.</param>
/// <param name="From">The query instant as recorded in the table, for the occurrence kinds; otherwise empty.</param>
/// <param name="Expected">
/// The expected result: one instant, instants separated by <c>|</c>, a comparison expression, or canonical text, by
/// kind.
/// </param>
/// <param name="Flags">The space-separated exclusion flags recorded in the table.</param>
/// <remarks>
/// Every source is a third-party comparison rather than an authority. Where a library's dialect differs from Vixie's,
/// the table either restates the row in Bodu's terms, saying so in its reference column, or flags the row and excludes
/// it; <c>corpus/recurrence/README.md</c> records each source's divergences.
/// </remarks>
public sealed record CronLibraryVectorKat(
    string Name,
    string Kind,
    CronFormat Format,
    string Expression,
    string From,
    string Expected,
    string Flags)
    : IKat
{
    /// <summary>The instant layouts a table may use, without and with fractional seconds.</summary>
    private static readonly string[] s_localFormats = ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"];

    /// <summary>The offset-bearing instant layouts a table may use, without and with fractional seconds.</summary>
    private static readonly string[] s_offsetFormats = ["yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"];

    /// <summary>
    /// Gets the recorded flags that place the row out of scope.
    /// </summary>
    /// <value>The flags, in table order.</value>
    public IEnumerable<string> ExclusionFlags =>
        Flags.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Gets a value indicating whether the row falls inside the surface this library models.
    /// </summary>
    /// <value><see langword="true" /> when the table recorded no exclusion flag.</value>
    public bool IsInScope =>
        !ExclusionFlags.Any();

    /// <summary>
    /// Gets a value indicating whether the row queries through the <see cref="DateTimeOffset" /> overloads.
    /// </summary>
    /// <value><see langword="true" /> when the recorded query instant carries an offset.</value>
    public bool HasOffset =>
        IsOffsetInstant(From);

    /// <summary>
    /// Gets the expected instants of an occurrence row, in the order the queries return them.
    /// </summary>
    /// <value>The instants recorded in <see cref="Expected" />, split at <c>|</c>.</value>
    public string[] ExpectedInstants =>
        Expected.Split('|');

    /// <summary>
    /// Parses a recorded instant that carries no offset.
    /// </summary>
    /// <param name="value">The instant text.</param>
    /// <returns>The wall-clock instant, of unspecified kind.</returns>
    public static DateTime ParseLocal(string value) =>
        DateTime.ParseExact(value, s_localFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);

    /// <summary>
    /// Parses a recorded instant that carries an offset.
    /// </summary>
    /// <param name="value">The instant text.</param>
    /// <returns>The instant with its offset.</returns>
    public static DateTimeOffset ParseOffset(string value) =>
        DateTimeOffset.ParseExact(value, s_offsetFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);

    /// <summary>
    /// Determines whether recorded instant text carries an offset.
    /// </summary>
    /// <param name="value">The instant text.</param>
    /// <returns><see langword="true" /> when the text ends in an offset; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The date and time occupy the first 19 characters, so a sign after them can only open an offset.
    /// </remarks>
    public static bool IsOffsetInstant(string value) =>
        value.Length > 19 && value.AsSpan(19).IndexOfAny('+', '-') >= 0;
}
