// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides null-tolerant ordering helpers — <c>Min</c>, <c>Max</c>, and <c>Coalesce</c> — that operate over any pair
/// of <see cref="IComparable{T}" /> or <see cref="Nullable{T}" /> values.
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
/// These helpers complement <see cref="IComparableExtensions" />, which provides instance-style comparison predicates (
/// <c>IsBetween</c>, <c>IsGreaterThan</c>, and so on). <see cref="ComparableHelper" /> is the static surface for
/// combining two operands; <see cref="IComparableExtensions" /> is the surface for testing a single operand against
/// bounds.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// int? a = 5;
/// int? b = null;
///
/// int? larger  = ComparableHelper.Max(a, b);       // 5
/// int? smaller = ComparableHelper.Min(a, b);       // 5 (null is skipped)
/// int? either  = ComparableHelper.Coalesce(b, a);  // 5
///
/// // Custom comparer for reverse-ordered strings.
/// string? winner = ComparableHelper.Max("alpha", "beta", StringComparer.OrdinalIgnoreCase); // "beta"
///]]>
/// </code>
/// </example>
public static partial class ComparableHelper
{
}
