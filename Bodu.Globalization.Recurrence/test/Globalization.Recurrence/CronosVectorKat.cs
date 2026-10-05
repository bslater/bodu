// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronosVectorKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents one cron vector derived from Cronos's test suite, together with the outcome that implementation asserts
/// for it.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Kind">
/// The assertion the row makes: <c>next</c>, <c>unreachable</c>, <c>invalid</c>, <c>equal</c>, <c>notEqual</c>, or
/// <c>toString</c>.
/// </param>
/// <param name="Format">The field layout the expression is parsed with.</param>
/// <param name="Expression">The cron text under test.</param>
/// <param name="From">The query instant, for the occurrence kinds.</param>
/// <param name="Expected">The expected result: an instant, a comparison expression, or canonical text, by kind.</param>
/// <param name="Flags">The space-separated scope flags recorded in the corpus.</param>
/// <remarks>
/// Cronos is a third-party comparison rather than an authority. It implements Quartz-flavoured cron and departs from
/// Vixie in several places, each of which the corpus flags and excludes rather than reconciles.
/// </remarks>
public sealed record CronosVectorKat(
    string Name,
    string Kind,
    CronFormat Format,
    string Expression,
    DateTime From,
    string Expected,
    string Flags)
    : IKat
{
    /// <summary>The flag that marks a row using the Quartz day tokens, which identifies the row without excluding it.</summary>
    private const string QuartzTokenFlag = "quartz-ext";

    /// <summary>
    /// Gets the recorded flags that place the vector out of scope.
    /// </summary>
    /// <value>Every recorded flag but <c>quartz-ext</c>.</value>
    /// <remarks>
    /// Every flag but <c>quartz-ext</c> is an exclusion, set only where Cronos and Bodu genuinely disagree.
    /// <c>quartz-ext</c> marks the rows that use the Quartz day tokens, which Bodu accepts as Cronos does; it excluded
    /// them until Bodu 1.3.0 and now only identifies them.
    /// </remarks>
    public IEnumerable<string> ExclusionFlags =>
        Flags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(flag => flag != QuartzTokenFlag);

    /// <summary>
    /// Gets a value indicating whether the vector falls inside the surface this library models.
    /// </summary>
    /// <value><see langword="true" /> when the corpus recorded no excluding flag.</value>
    public bool IsInScope =>
        !ExclusionFlags.Any();
}
