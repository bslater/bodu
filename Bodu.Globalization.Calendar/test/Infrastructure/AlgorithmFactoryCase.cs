// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmFactoryCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Identifies a single <see cref="INotableDateAlgorithm" /> implementation under test by pairing a short
/// human-readable label with a factory delegate that constructs a fresh instance per row.
/// </summary>
/// <remarks>
/// <para>
/// Used by <c>NotableDateAlgorithmContractTests</c> to drive the boundary contract (year &lt; 1, year &gt; 9999,
/// null/Gregorian/Julian calendar) across every shipped algorithm from a single set of <c>[DataTestMethod]</c>
/// declarations. A new factory entry in <see cref="NotableDateAlgorithmKnownAnswers.AllAlgorithmFactories" />
/// automatically enrolls the corresponding algorithm in all boundary tests.
/// </para>
/// <para>
/// The delegate is invoked on every row so test isolation is preserved — no shared instance is mutated across
/// rows, which keeps the per-instance caches inside algorithms such as
/// <see cref="EasterSundayNotableDateAlgorithm" /> from leaking state between assertions.
/// </para>
/// </remarks>
public sealed record AlgorithmFactoryCase : IKat
{
    /// <inheritdoc />
    /// <remarks>Returns <see cref="Algorithm" /> so the row participates in <see cref="KatDisplayName" />-driven formatting alongside the existing <see cref="NotableDateAlgorithmKnownAnswers.GetDisplayName" /> path.</remarks>
    string IKat.Name => Algorithm;

    /// <summary>
    /// Gets the short label rendered by <see cref="NotableDateAlgorithmKnownAnswers.GetDisplayName" /> for
    /// rows produced from this case — for example <c>"Losar"</c>, <c>"Easter"</c>, or
    /// <c>"HinduLunar(Diwali)"</c>.
    /// </summary>
    /// <returns>The algorithm label, used in MSTest test explorer entries.</returns>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Gets the delegate that constructs a fresh <see cref="INotableDateAlgorithm" /> instance for one row.
    /// </summary>
    /// <returns>A factory that returns a newly allocated algorithm per invocation.</returns>
    public required Func<INotableDateAlgorithm> Factory { get; init; }
}
