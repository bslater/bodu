// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensions.AsReadOnly.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class SpanExtensions
{
    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}" /> view over the same memory as <paramref name="source" />, preventing
    /// callers from mutating the underlying data.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="source">The mutable span to make read-only.</param>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}" /> with the same backing memory, offset, and length as <paramref name="source" />.
    /// No data is copied.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is a zero-cost wrapper around the implicit conversion from <see cref="Span{T}" /> to
    /// <see cref="ReadOnlySpan{T}" /> defined by the runtime. The compiler erases the call entirely — the emitted IL is
    /// identical to writing <c>(ReadOnlySpan&lt;T&gt;)source</c> at the call site.
    /// </para>
    /// <para>
    /// The primary use case is readability and intent: passing a mutable span to a method that should only read from
    /// it, or storing it in a field or local that should not allow writes, without scattering explicit casts throughout
    /// the call site.
    /// </para>
    /// <para>
    /// This follows the same pattern as <see cref="MemoryExtensions.AsMemory{T}(T[])" />, which provides a named,
    /// intent-expressing wrapper over an implicit conversion rather than requiring callers to write casts directly.
    /// </para>
    /// </remarks>
    public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> source) => source;
}