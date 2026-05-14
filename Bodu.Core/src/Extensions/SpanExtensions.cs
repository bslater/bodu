// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;
/// <summary>
/// Provides span-shaping helpers — read-only conversion and windowed reversal — for code that already operates on
/// <see cref="Span{T}"/> or <see cref="ReadOnlySpan{T}"/> and wants to keep the underlying memory allocation-free.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Span{T}"/> is the right tool for stack-allocated, zero-copy work, but the BCL only ships a small number of
/// transformations on it directly. This class adds the two operations that hot-path code most often hand-rolls: a free
/// <see cref="Span{T}"/> -&gt; <see cref="ReadOnlySpan{T}"/> conversion that makes intent visible to callers, and a family of
/// in-place reversal operators that accept either an index/count pair or a <see cref="Range"/> — matching the surface used by
/// <c>ArrayExtensions.Reverse</c> so the same call shape works regardless of whether the caller holds a span or an array.
/// </para>
/// <para>
/// The API surface is intentionally narrow: <c>AsReadOnly</c> for capability-narrowing, and overloaded <c>Reverse</c> methods on
/// both <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> for whole-span and windowed reversal. The
/// <see cref="ReadOnlySpan{T}"/> overloads return a writeable <see cref="Span{T}"/> over the same memory, since reversal
/// inherently mutates and the caller must opt in to that.
/// </para>
/// <para>
/// All methods are <c>ref struct</c>-friendly, allocation-free, and operate in place; reversing a partial window leaves elements
/// outside the window untouched. The methods are not thread-safe — concurrent access to the underlying buffer must be
/// synchronized externally.
/// </para>
/// <example>
/// <code language="csharp">
/// Span&lt;int&gt; buffer = stackalloc int[] { 1, 2, 3, 4, 5, 6 };
///
/// // Reverse only the trailing window using a Range.
/// buffer.Reverse(2..);
/// // => buffer is now { 1, 2, 6, 5, 4, 3 }
///
/// // Narrow the surface before handing the span to a read-only consumer.
/// ReadOnlySpan&lt;int&gt; view = buffer.AsReadOnly();
///
/// // Reverse the full span back to its original ordering.
/// buffer.Reverse();
/// // => buffer is now { 3, 4, 5, 6, 2, 1 }
/// </code>
/// </example>
/// </remarks>
public static partial class SpanExtensions
{ }
