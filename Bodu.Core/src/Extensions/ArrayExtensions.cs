// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides allocation-aware operations over <see cref="Array" /> instances — clearing, copying, slicing, and reversing
/// — that either match the missing primitives on <see cref="Array" /> or supply more ergonomic counterparts to the BCL
/// helpers.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Array" /> type ships with a useful but uneven surface: <see cref="Array.Reverse(System.Array)" />
/// mutates in place while LINQ's <c>Reverse</c> allocates a new sequence;
/// <see cref="Array.Clear(System.Array, int, int)" /> requires explicit bounds; and there is no <c>Slice</c> for plain
/// arrays at all. This class fills those gaps with overload sets that work both on the strongly typed <c>T[]</c> form
/// and the non-generic <see cref="Array" /> base, so callers can choose the version that matches the data they already
/// hold.
/// </para>
/// <para>
/// The API surface is grouped around four operations: bulk-reset (<c>Clear</c>), shallow duplication (<c>Copy</c>),
/// windowed views returned as freshly allocated arrays (<c>Slice</c>), and whole-array or windowed element reversal (
/// <c>Reverse</c>). Each operation accepts a default whole-array form together with an indexed-range and
/// <see cref="Range" />-based overload, giving the same shape of API regardless of how the caller prefers to express
/// the window.
/// </para>
/// <para>
/// All methods validate their inputs through <see cref="ThrowHelper" /> and reject <see langword="null" /> arrays with
/// <see cref="ArgumentNullException" />. <c>Reverse</c>, <c>Slice</c>, and <c>Copy</c> always allocate: each returns a
/// new array and leaves the source untouched — <c>Reverse</c> copies the full source and reverses the nominated window
/// on the copy, unlike the in-place <see cref="Array.Reverse(System.Array)" />. Only <c>Clear</c> mutates the source
/// array. Operations are not thread-safe — callers are responsible for synchronizing access to a shared array.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// int[] source = { 1, 2, 3, 4, 5, 6 };
///
/// // Reverse only the middle window — returns a new array; source is unchanged.
/// int[] reversed = source.Reverse(1, 4); // => { 1, 5, 4, 3, 2, 6 }
///
/// // Take a freshly allocated slice using a Range expression.
/// int[] tail = source.Slice(2..); // => { 3, 4, 5, 6 }
///
/// // Clear the prefix in place without touching the tail.
/// source.Clear(0, 3); // => source is now { 0, 0, 0, 4, 5, 6 }
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class ArrayExtensions
{ }