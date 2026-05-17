// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensions.Reverse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class SpanExtensions
{
    /// <summary>
    /// Creates a new array containing all elements of <paramref name="source" /> in reverse order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="source">The span whose elements will be reversed.</param>
    /// <returns>
    /// A new <see cref="Span{T}" /> backed by a heap-allocated array containing all elements of
    /// <paramref name="source" /> in reverse order.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="Reverse{T}(ReadOnlySpan{T}, int, int)" /> with <c>start = 0</c> and
    /// <c>length = source.Length</c>. See that overload for full performance and allocation details.
    /// </remarks>
    public static Span<T> Reverse<T>(this ReadOnlySpan<T> source)
        => ReverseCore(source, 0, source.Length);

    /// <summary>
    /// Creates a new array that is a copy of <paramref name="source" /> with elements in the specified range reversed,
    /// leaving all other elements in their original positions.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="source">The span to copy and partially reverse.</param>
    /// <param name="index">
    /// The zero-based index of the first element in the range to reverse. Must be non-negative and less than or equal
    /// to <see cref="ReadOnlySpan{T}.Length" />.
    /// </param>
    /// <param name="count">
    /// The number of elements to reverse. Must be non-negative and, together with <paramref name="index" />, must not
    /// exceed <see cref="ReadOnlySpan{T}.Length" />. A value of zero or one results in a straight copy with no reversal
    /// performed.
    /// </param>
    /// <returns>
    /// A new <see cref="Span{T}" /> of the same length as <paramref name="source" />, where elements in the range [
    /// <paramref name="index" />, <paramref name="index" /> + <paramref name="count" />) appear in reverse order and
    /// all other elements are copied unchanged.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> is negative or greater than <see cref="ReadOnlySpan{T}.Length" />, or when
    /// <paramref name="count" /> is negative or would extend the range beyond the end of <paramref name="source" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the canonical public implementation. The full-span overload (<see cref="Reverse{T}(ReadOnlySpan{T})" />)
    /// and the <see cref="Range" />-based overload (<see cref="Reverse{T}(ReadOnlySpan{T}, Range)" />) both resolve
    /// their arguments and delegate here after validation.
    /// </para>
    /// <para>
    /// Bounds validation uses unsigned comparison so that a single branch catches both negative values and values that
    /// exceed the span length, matching the pattern used throughout <see cref="MemoryExtensions" />.
    /// </para>
    /// <para>
    /// Once validated, execution is forwarded to <see cref="ReverseCore{T}" />, which performs the allocation and the
    /// SIMD-accelerated copy and reversal.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code> int[] data = { 1, 2, 3, 4, 5 }; // Reverse a middle section; first and last elements are untouched.
    /// Span&lt;int&gt; result = data.AsSpan().Reverse(start: 1, length: 3); // [ 1, 4, 3, 2, 5 ] </code>
    /// </example>
    public static Span<T> Reverse<T>(this ReadOnlySpan<T> source, int index, int count)
    {
        ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(source, index, count);

        return ReverseCore(source, index, count);
    }

    /// <summary>
    /// Creates a new array that is a copy of <paramref name="source" /> with elements in the specified
    /// <see cref="Range" /> reversed, leaving all other elements in their original positions.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="source">The span to copy and partially reverse.</param>
    /// <param name="range">
    /// The range of elements to reverse. Both start-relative (e.g. <c>1..4</c>) and end-relative (e.g. <c>^4..^1</c>)
    /// index expressions are supported. A range covering zero or one element results in a straight copy with no
    /// reversal performed.
    /// </param>
    /// <returns>
    /// A new <see cref="Span{T}" /> of the same length as <paramref name="source" />, where elements within
    /// <paramref name="range" /> appear in reverse order and all elements outside <paramref name="range" /> are copied
    /// unchanged.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="range" /> resolves to a start index that is negative or to an end index that exceeds
    /// <see cref="ReadOnlySpan{T}.Length" />. Resolution is performed by <see cref="Range.GetOffsetAndLength" />; the
    /// resulting offset and length are then validated and forwarded to
    /// <see cref="Reverse{T}(ReadOnlySpan{T}, int, int)" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Resolves <paramref name="range" /> to an offset and length via <see cref="Range.GetOffsetAndLength" /> and
    /// delegates to <see cref="Reverse{T}(ReadOnlySpan{T}, int, int)" />. Prefer that overload when the start and
    /// length are already available as integers to avoid constructing an intermediate <see cref="Range" /> value.
    /// </para>
    /// <para>
    /// This overload is most ergonomic when the range is expressed as a C# 8+ index expression at the call site (e.g.
    /// <c>1..4</c> or <c>^3..^1</c>), where the compiler handles <see cref="Range" /> construction automatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code> int[] data = { 1, 2, 3, 4, 5 }; Span&lt;int&gt; fromStart = data.AsSpan().Reverse(1..4); // [ 1, 4, 3, 2,
    /// 5 ] Span&lt;int&gt; fromEnd = data.AsSpan().Reverse(^4..^1); // [ 1, 4, 3, 2, 5 ] </code>
    /// </example>
    public static Span<T> Reverse<T>(this ReadOnlySpan<T> source, Range range)
    {
        (var start, var length) = range.GetOffsetAndLength(source.Length);
        return ReverseCore(source, start, length);
    }

    // -------------------------------------------------------------------------
    // Span<T> — convenience overloads (forward to ReadOnlySpan<T>)
    // -------------------------------------------------------------------------

    /// <inheritdoc cref="Reverse{T}(ReadOnlySpan{T})" />
    /// <remarks>
    /// Convenience overload for mutable spans. Implicitly converts <paramref name="source" /> to
    /// <see cref="ReadOnlySpan{T}" /> and delegates to <see cref="Reverse{T}(ReadOnlySpan{T})" />.
    /// </remarks>
    public static Span<T> Reverse<T>(this Span<T> source)
        => Reverse((ReadOnlySpan<T>)source);

    /// <inheritdoc cref="Reverse{T}(ReadOnlySpan{T}, int, int)" />
    /// <remarks>
    /// Convenience overload for mutable spans. Implicitly converts <paramref name="source" /> to
    /// <see cref="ReadOnlySpan{T}" /> and delegates to <see cref="Reverse{T}(ReadOnlySpan{T}, int, int)" />.
    /// </remarks>
    public static Span<T> Reverse<T>(this Span<T> source, int index, int count)
        => Reverse((ReadOnlySpan<T>)source, index, count);

    /// <inheritdoc cref="Reverse{T}(ReadOnlySpan{T}, Range)" />
    /// <remarks>
    /// Convenience overload for mutable spans. Implicitly converts <paramref name="source" /> to
    /// <see cref="ReadOnlySpan{T}" /> and delegates to <see cref="Reverse{T}(ReadOnlySpan{T}, Range)" />.
    /// </remarks>
    public static Span<T> Reverse<T>(this Span<T> source, Range range)
        => Reverse((ReadOnlySpan<T>)source, range);

    // -------------------------------------------------------------------------
    // Core implementation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Allocates a copy of <paramref name="source" /> and reverses the slice defined by <paramref name="index" /> and
    /// <paramref name="count" /> in-place on the copy.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="source">The span to copy. Must not be modified during the operation.</param>
    /// <param name="index">
    /// The zero-based index of the first element to reverse within the copied array. Must be a valid, pre-validated
    /// index into <paramref name="source" />.
    /// </param>
    /// <param name="count">
    /// The number of elements to reverse. Must be a valid, pre-validated length relative to <paramref name="index" />
    /// and <paramref name="source" />.
    /// </param>
    /// <returns>
    /// A new <see cref="Span{T}" /> backed by a heap-allocated array containing all elements of
    /// <paramref name="source" />, with the slice [<paramref name="index" />, <paramref name="index" /> +
    /// <paramref name="count" />) reversed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method assumes all parameters are valid and skips all validation checks. Intended for internal use where
    /// input validity is already ensured.
    /// </para>
    /// <para>
    /// The full contents of <paramref name="source" /> are copied into a new heap-allocated array via
    /// <see cref="ReadOnlySpan{T}.CopyTo" />, then the slice corresponding to [<paramref name="index" />,
    /// <paramref name="index" /> + <paramref name="count" />) is reversed in-place via
    /// <see cref="MemoryExtensions.Reverse{T}(Span{T})" />. Both operations delegate to hardware-accelerated SIMD paths
    /// (AVX-512, AVX2, SSE) where available, falling back to a scalar swap loop for remainder elements or unsupported
    /// hardware.
    /// </para>
    /// <para>
    /// For unmanaged element types, zero-initialization of the backing array is elided via
    /// <see cref="GC.AllocateUninitializedArray{T}" />, since <see cref="ReadOnlySpan{T}.CopyTo" /> overwrites every
    /// element before the array is returned.
    /// </para>
    /// <para>
    /// The <c>length &gt; 1</c> guard avoids entering the SIMD dispatch machinery for degenerate slices (empty or
    /// single element), which would be wasted overhead.
    /// </para>
    /// </remarks>
    internal static Span<T> ReverseCore<T>(ReadOnlySpan<T> source, int index, int count)
        => (Span<T>)ArrayExtensions.ReverseCore(source, index, count);
}
