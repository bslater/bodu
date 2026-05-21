// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDictionaryExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Provides retrieval-and-mutation helpers for <see cref="IDictionary{TKey, TValue}" /> — get-or-add and add-or-update
/// operations modelled on the <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}" /> surface
/// but available on any dictionary implementation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IDictionary{TKey, TValue}" /> exposes only primitive lookup and mutation members, so callers routinely
/// hand-roll the "return the existing value or insert a new one" pattern. This class collects those patterns behind
/// verb-led names.
/// </para>
/// <para>
/// These helpers perform no synchronization. On a dictionary shared between threads the caller must provide external
/// synchronization; they are not a substitute for
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}" /> when concurrent access is required.
/// </para>
/// </remarks>
public static partial class IDictionaryExtensions
{
}
