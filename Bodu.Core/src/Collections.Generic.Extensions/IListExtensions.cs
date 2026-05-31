// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Provides extension methods that add predicate-based search, safe relocation, and bulk replacement operations to any
/// type implementing <see cref="IList{T}" />.
/// </summary>
/// <remarks>
/// <para>
/// The extension surface fills gaps in the BCL <see cref="IList{T}" /> contract. Predicate variants of
/// <see cref="System.Collections.Generic.List{T}.IndexOf(T)" /> and
/// <see cref="System.Collections.Generic.List{T}.LastIndexOf(T)" /> let callers locate elements by an arbitrary
/// condition without first projecting the list through LINQ; <c>TryMove</c> and <c>TrySwap</c> reorder elements
/// in-place with bounds-checked, non-throwing semantics; and <c>ReplaceAll</c> performs an in-place value substitution
/// across the entire list (or matched subset) and returns the number of replacements made.
/// </para>
/// <para>
/// All methods operate directly against the supplied list — no copy is allocated, and the original ordering of
/// non-affected elements is preserved. Methods that mutate require the list to be writable; methods that only search
/// are safe to call on read-only views provided the list still implements <see cref="IList{T}" />.
/// </para>
/// <para>
/// Equality-based methods accept an optional <see cref="IEqualityComparer{T}" />. When omitted,
/// <see cref="EqualityComparer{T}.Default" /> is used, matching the BCL convention so behaviour is consistent with the
/// rest of the framework.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// IList<string> items = new List<string> { "alpha", "beta", "gamma", "beta" };
///
/// Predicate-based search.
/// int firstBetaIndex = items.IndexOf(s => s.StartsWith("b")); // 1
///
/// Try-style relocation that won't throw on out-of-range indices.
/// if (items.TryMove(oldIndex: 0, newIndex: 2))
///     Console.WriteLine(string.Join(", ", items)); // beta, gamma, alpha, beta
///
/// Bulk replacement with a count of changes.
/// int replaced = items.ReplaceAll(oldItem: "beta", newItem: "BETA"); // 2
///]]>
/// </example>
public static partial class IListExtensions
{ }
