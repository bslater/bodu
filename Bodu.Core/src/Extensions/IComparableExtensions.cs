// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides predicate, clamping, and selection operations over any <see cref="IComparable{T}" /> value, turning common
/// "is this within the expected window?" and "pick the better candidate" decisions into one-liners that read naturally.
/// </summary>
/// <remarks>
/// <para>
/// Range checks and bounded arithmetic are some of the most repeated patterns in business code, yet expressing them
/// with raw <see cref="IComparable{T}.CompareTo(T)" /> is verbose and easy to get wrong (off-by-one boundaries, swapped
/// arguments, missing <see cref="IComparer{T}" /> threads). This class wraps those checks behind verb-led names that
/// match the way callers describe the intent — <c>IsBetween</c>, <c>IsOutside</c>, <c>AtLeast</c>, <c>AtMost</c>,
/// <c>Clamp</c>, <c>Min</c>, <c>Max</c> — so the call site reads as a fluent assertion rather than a comparison
/// expression.
/// </para>
/// <para>
/// The API surface is split into three groups: comparison predicates (<c>IsLessThan</c>, <c>IsLessThanOrEqual</c>,
/// <c>IsGreaterThan</c>, <c>IsGreaterThanOrEqual</c>, <c>IsEqualTo</c>), windowed predicates (<c>IsBetween</c>,
/// <c>IsOutside</c>), and value-shaping helpers that produce a result rather than a boolean (<c>AtLeast</c>,
/// <c>AtMost</c>, <c>Clamp</c>, <c>Min</c>, <c>Max</c>). Each method is offered with a default overload that uses the
/// type's natural ordering and a paired overload that accepts an explicit <see cref="IComparer{T}" />, so callers can
/// opt into culture-aware string comparisons or domain-specific orderings without rewriting the call.
/// </para>
/// <para>
/// All methods are pure, allocation-free, and deterministic. The default-comparer overloads use
/// <see cref="Comparer{T}.Default" />, which itself defers to <see cref="IComparable{T}" /> when implemented; if the
/// type is not orderable an <see cref="InvalidOperationException" /> bubbles up from the underlying comparer. Boundary
/// inclusivity follows the inclusive convention: <c>IsBetween</c> includes both endpoints, and <c>Clamp</c> returns the
/// boundary value rather than throwing when the input is outside.
/// </para>
/// <example>
/// <code language="csharp"><![CDATA[ int volume = 11; // Replace explicit min/max calls with a fluent clamp. int safeVolume =
/// volume.Clamp(0, 10); // => 10 // Inclusive "between" predicate, ideal in guard clauses. bool inDecimalDigits =
/// '7'.IsBetween('0', '9'); // => true // Custom comparer for case-insensitive string ranges. bool inMidAlphabet =
/// "Mango".IsBetween("apple", "orange", StringComparer.OrdinalIgnoreCase); // => true ]]></code>
/// </example>
/// </remarks>
public static partial class IComparableExtensions
{
}