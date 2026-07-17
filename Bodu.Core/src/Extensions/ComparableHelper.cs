// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides null-tolerant ordering helpers — <c>Min</c>, <c>Max</c>, and <c>Coalesce</c> — for pairs of operands.
/// <c>Min</c> and <c>Max</c> require <see cref="IComparable{T}" /> operands (reference types may be
/// <see langword="null" />); <c>Coalesce</c> is unconstrained and additionally accepts <see cref="Nullable{T}" />
/// value types.
/// </summary>
/// <remarks>
/// <para>
/// The helpers are designed for the common case in which one or both operands may be <see langword="null" /> and the
/// caller wants the non-null value to win rather than to throw. <c>Coalesce</c> returns the first non-null operand and
/// is equivalent to the null-coalescing operator over reference and nullable value types. <c>Min</c> and <c>Max</c>
/// return the smaller or larger operand respectively; when one operand is <see langword="null" /> the other is returned
/// unchanged, and when both are <see langword="null" /> the result is <see langword="null" />.
/// </para>
/// <para>
/// Each method accepts an optional <see cref="IComparer{T}" /> override; when omitted,
/// <see cref="Comparer{T}.Default" /> is used. Because the ordering call only occurs when both operands are non-null,
/// callers can supply comparers that throw on <see langword="null" /> input without guarding separately.
/// </para>
/// <para>
/// These helpers complement <see cref="ComparableExtensions" />, which provides instance-style comparison predicates (
/// <c>IsBetween</c>, <c>IsGreaterThan</c>, and so on). <see cref="ComparableHelper" /> is the static surface for
/// combining two operands; <see cref="ComparableExtensions" /> is the surface for testing a single operand against
/// bounds.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// string? a = "alpha";
/// string? b = null;
///
/// string? larger  = ComparableHelper.Max(a, b);       // "alpha"
/// string? smaller = ComparableHelper.Min(a, b);       // "alpha" (null is skipped)
/// string? either  = ComparableHelper.Coalesce(b, a);  // "alpha"
///
/// // Coalesce alone is unconstrained, so it also accepts nullable value types.
/// int? first = ComparableHelper.Coalesce((int?)null, 5); // 5
///
/// // Custom comparer for case-insensitive string ordering.
/// string? winner = ComparableHelper.Max("alpha", "Beta", StringComparer.OrdinalIgnoreCase); // "Beta"
///]]>
/// </code>
/// </example>
public static partial class ComparableHelper
{
}
