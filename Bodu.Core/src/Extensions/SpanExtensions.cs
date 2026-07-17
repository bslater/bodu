// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides span-shaping helpers — read-only conversion and copy-and-reverse operations — for code that already
/// operates on <see cref="Span{T}" /> or <see cref="ReadOnlySpan{T}" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Span{T}" /> is the right tool for stack-allocated, zero-copy work, but the BCL only ships a small number
/// of transformations on it directly. This class adds two operations: a free <see cref="Span{T}" /> -&gt;
/// <see cref="ReadOnlySpan{T}" /> conversion that makes intent visible to callers, and a family of copy-and-reverse
/// operators that accept either an index/count pair or a <see cref="Range" /> — matching the surface used by
/// <c>ArrayExtensions.Reverse</c> so the same call shape works regardless of whether the caller holds a span or an
/// array.
/// </para>
/// <para>
/// The API surface is intentionally narrow: <c>AsReadOnly</c> for capability-narrowing, and overloaded <c>Reverse</c>
/// methods on both <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> for whole-span and windowed reversal.
/// Unlike the in-place <see cref="MemoryExtensions.Reverse{T}(Span{T})" />, every <c>Reverse</c> overload copies the
/// full source into a newly heap-allocated array and reverses the nominated window on that copy; the returned
/// <see cref="Span{T}" /> wraps the fresh allocation, and the source memory is never modified. This is also why the
/// <see cref="ReadOnlySpan{T}" /> overloads can return a writeable <see cref="Span{T}" /> — it aliases the copy, not
/// the read-only source.
/// </para>
/// <para>
/// <c>AsReadOnly</c> is allocation-free; the <c>Reverse</c> overloads allocate one array per call. Reversing a partial
/// window copies all elements and reverses only those inside the window. The methods are not thread-safe — concurrent
/// access to the underlying buffer must be synchronized externally.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// Span<int> buffer = stackalloc int[] { 1, 2, 3, 4, 5, 6 };
///
/// // Reverse only the trailing window using a Range — returns a new heap-backed span.
/// Span<int> reversed = buffer.Reverse(2..); // => { 1, 2, 6, 5, 4, 3 }; buffer is unchanged
///
/// // Narrow the surface before handing the span to a read-only consumer.
/// ReadOnlySpan<int> view = buffer.AsReadOnly();
///
/// // Reverse the full span — again into a fresh copy.
/// Span<int> full = buffer.Reverse(); // => { 6, 5, 4, 3, 2, 1 }; buffer is unchanged
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class SpanExtensions
{ }
