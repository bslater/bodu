// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.Reverse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class ArrayExtensions
{
    /// <summary>
    /// Creates a new array containing all elements of <paramref name="source"/>
    /// in reverse order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="source">The array whose elements will be reversed.</param>
    /// <returns>
    /// A new <typeparamref name="T"/>[] containing all elements of
    /// <paramref name="source"/> in reverse order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Converts <paramref name="source"/> to a <see cref="ReadOnlySpan{T}"/> via
    /// <see cref="MemoryExtensions.AsSpan{T}(T[])"/> and delegates to
    /// <see cref="ReverseCore{T}"/>, which performs the SIMD-accelerated copy and
    /// reversal. See <see cref="Reverse{T}(T[], int, int)"/> for full performance
    /// and allocation details.
    /// </remarks>
    public static T[] Reverse<T>(this T[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ReverseCore<T>(source, 0, source.Length);
    }

    /// <summary>
    /// Creates a new array that is a copy of <paramref name="source"/> with elements
    /// in the specified range reversed, leaving all other elements in their original positions.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="source">The array to copy and partially reverse.</param>
    /// <param name="index">
    /// The zero-based index of the first element in the range to reverse. Must be
    /// non-negative and less than or equal to <c>source.Length</c>.
    /// </param>
    /// <param name="count">
    /// The number of elements to reverse. Must be non-negative and, together with
    /// <paramref name="index"/>, must not exceed <c>source.Length</c>. A value of
    /// zero or one results in a straight copy with no reversal performed.
    /// </param>
    /// <returns>
    /// A new <typeparamref name="T"/>[] of the same length as <paramref name="source"/>,
    /// where elements in [<paramref name="index"/>, <paramref name="index"/> +
    /// <paramref name="count"/>) appear in reverse order and all other elements are
    /// copied unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is negative or greater than
    /// <c>source.Length</c>, or when <paramref name="count"/> is negative or
    /// would extend the range beyond the end of <paramref name="source"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the canonical typed-array implementation. The full-array overload
    /// (<see cref="Reverse{T}(T[])"/>) and the <see cref="Range"/>-based overload
    /// (<see cref="Reverse{T}(T[], Range)"/>) both resolve their arguments and
    /// delegate here.
    /// </para>
    /// <para>
    /// Bounds validation is delegated to
    /// <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid{T}(ReadOnlySpan{T}, int, int, string, string, string)"/>,
    /// which uses unsigned comparison to catch both negative values and out-of-bounds
    /// values in a single branch. Validated arguments are then forwarded to
    /// <see cref="ReverseCore{T}"/>, which performs the SIMD-accelerated copy and
    /// in-place reversal of the nominated slice.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// int[] data = { 1, 2, 3, 4, 5 };
    ///
    /// int[] full    = data.Reverse();              // [ 5, 4, 3, 2, 1 ]
    /// int[] partial = data.Reverse(index: 1, count: 3); // [ 1, 4, 3, 2, 5 ]
    /// </code>
    /// </example>
    public static T[] Reverse<T>(this T[] source, int index, int count)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(source, index, count);
        return ReverseCore<T>(source, index, count);
    }

    /// <summary>
    /// Creates a new array that is a copy of <paramref name="source"/> with elements
    /// in the specified <see cref="Range"/> reversed, leaving all other elements in
    /// their original positions.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="source">The array to copy and partially reverse.</param>
    /// <param name="range">
    /// The range of elements to reverse. Both start-relative (e.g. <c>1..4</c>) and
    /// end-relative (e.g. <c>^4..^1</c>) index expressions are supported. A range
    /// covering zero or one element results in a straight copy with no reversal performed.
    /// </param>
    /// <returns>
    /// A new <typeparamref name="T"/>[] of the same length as <paramref name="source"/>,
    /// where elements within <paramref name="range"/> appear in reverse order and all
    /// elements outside <paramref name="range"/> are copied unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="range"/> resolves to a start index that is negative
    /// or to an end index that exceeds <c>source.Length</c>. Resolution is performed by
    /// <see cref="Range.GetOffsetAndLength"/>; the resulting values are forwarded directly
    /// to <see cref="ReverseCore{T}"/> without a second validation pass.
    /// </exception>
    /// <remarks>
    /// Resolves <paramref name="range"/> via <see cref="Range.GetOffsetAndLength"/> and
    /// delegates to <see cref="ReverseCore{T}"/>. Prefer
    /// <see cref="Reverse{T}(T[], int, int)"/> when the start and count are already
    /// available as integers to avoid constructing an intermediate <see cref="Range"/>
    /// value.
    /// </remarks>
    /// <example>
    /// <code>
    /// int[] data = { 1, 2, 3, 4, 5 };
    ///
    /// int[] fromStart = data.Reverse(1..4);   // [ 1, 4, 3, 2, 5 ]
    /// int[] fromEnd   = data.Reverse(^4..^1); // [ 1, 4, 3, 2, 5 ]
    /// </code>
    /// </example>
    public static T[] Reverse<T>(this T[] source, Range range)
    {
        ThrowHelper.ThrowIfNull(source);
        var (start, length) = range.GetOffsetAndLength(source.Length);
        return ReverseCore<T>(source, start, length);
    }

    // -------------------------------------------------------------------------
    // Array — non-generic overloads
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new single-dimensional array containing all elements of
    /// <paramref name="source"/> in reverse order.
    /// </summary>
    /// <param name="source">The array whose elements will be reversed.</param>
    /// <returns>
    /// A new <see cref="Array"/> of the same element type and length as
    /// <paramref name="source"/>, with all elements in reverse order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source"/> is not a single-dimensional array.
    /// <see cref="Array.Reverse(Array, int, int)"/>, which is used internally, does
    /// not support multidimensional arrays.
    /// </exception>
    /// <remarks>
    /// Delegates to <see cref="Reverse(Array, int, int)"/> with <c>index = 0</c>
    /// and <c>count = source.Length</c>. See that overload for full implementation
    /// details. Prefer the generic <see cref="Reverse{T}(T[])"/> overload where the
    /// element type is known at compile time; the non-generic path cannot use
    /// <see cref="ReverseCore{T}"/> and falls back to <see cref="Array.Copy(Array, Array, int)"/> and
    /// <see cref="Array.Reverse(Array, int, int)"/>.
    /// </remarks>
    public static Array Reverse(this Array source)
    {
        ThrowHelper.ThrowIfNull(source);
        return ReverseArrayCore(source, 0, source.Length);
    }

    /// <summary>
    /// Creates a new single-dimensional array that is a copy of <paramref name="source"/>
    /// with elements in the specified range reversed, leaving all other elements in their
    /// original positions.
    /// </summary>
    /// <param name="source">The array to copy and partially reverse.</param>
    /// <param name="index">
    /// The zero-based index of the first element in the range to reverse. Must be
    /// non-negative and less than or equal to <c>source.Length</c>.
    /// </param>
    /// <param name="count">
    /// The number of elements to reverse. Must be non-negative and, together with
    /// <paramref name="index"/>, must not exceed <c>source.Length</c>. A value of
    /// zero or one results in a straight copy with no reversal performed.
    /// </param>
    /// <returns>
    /// A new <see cref="Array"/> of the same element type and length as
    /// <paramref name="source"/>, where elements in [<paramref name="index"/>,
    /// <paramref name="index"/> + <paramref name="count"/>) appear in reverse order
    /// and all other elements are copied unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source"/> is not a single-dimensional array, or
    /// when <c>index + count</c> exceeds <c>source.Length</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> or <paramref name="count"/> is negative
    /// or exceeds <c>source.Length</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the canonical non-generic implementation. The full-array overload
    /// (<see cref="Reverse(Array)"/>) and the <see cref="Range"/>-based overload
    /// (<see cref="Reverse(Array, Range)"/>) both resolve their arguments and
    /// delegate here.
    /// </para>
    /// <para>
    /// Unlike the typed <see cref="Reverse{T}(T[], int, int)"/> overload, this method
    /// cannot delegate to <see cref="ReverseCore{T}"/> because the element type is not
    /// known at compile time. Instead it uses <see cref="Array.CreateInstance(Type, int)"/> to
    /// allocate a correctly typed result, <see cref="Array.Copy(Array, Array, int)"/> to populate it, and
    /// <see cref="Array.Reverse(Array, int, int)"/> to reverse the nominated slice
    /// in-place on the copy. These BCL methods are used internally via
    /// <see cref="ReverseArrayCore"/>.
    /// </para>
    /// <para>
    /// Bounds validation is delegated to
    /// <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid"/>, which also guards
    /// against <see langword="null"/>. The rank check is performed separately because
    /// <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid"/> does not inspect
    /// <see cref="Array.Rank"/>.
    /// </para>
    /// </remarks>
    public static Array Reverse(this Array source, int index, int count)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(source, index, count);
        ThrowHelper.ThrowIfArrayMultidimensional(source);
        return ReverseArrayCore(source, index, count);
    }

    /// <summary>
    /// Creates a new single-dimensional array that is a copy of <paramref name="source"/>
    /// with elements in the specified <see cref="Range"/> reversed, leaving all other
    /// elements in their original positions.
    /// </summary>
    /// <param name="source">The array to copy and partially reverse.</param>
    /// <param name="range">
    /// The range of elements to reverse. Both start-relative (e.g. <c>1..4</c>) and
    /// end-relative (e.g. <c>^4..^1</c>) index expressions are supported. A range
    /// covering zero or one element results in a straight copy with no reversal performed.
    /// </param>
    /// <returns>
    /// A new <see cref="Array"/> of the same element type and length as
    /// <paramref name="source"/>, where elements within <paramref name="range"/> appear
    /// in reverse order and all elements outside <paramref name="range"/> are copied
    /// unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source"/> is not a single-dimensional array.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="range"/> resolves to a start index that is negative
    /// or to an end index that exceeds <c>source.Length</c>. Resolution is performed
    /// by <see cref="Range.GetOffsetAndLength"/>.
    /// </exception>
    /// <remarks>
    /// Resolves <paramref name="range"/> via <see cref="Range.GetOffsetAndLength"/> and
    /// delegates to <see cref="ReverseArrayCore"/>. Prefer
    /// <see cref="Reverse(Array, int, int)"/> when the start and count are already
    /// available as integers to avoid constructing an intermediate <see cref="Range"/>
    /// value.
    /// </remarks>
    public static Array Reverse(this Array source, Range range)
    {
        ThrowHelper.ThrowIfArrayMultidimensional(source);
        var (start, length) = range.GetOffsetAndLength(source.Length);
        return ReverseArrayCore(source, start, length);
    }

    // -------------------------------------------------------------------------
    // Non-generic core and shared guard
    // -------------------------------------------------------------------------

    /// <summary>
    /// Allocates a copy of <paramref name="source"/> and reverses the slice defined by
    /// <paramref name="index"/> and <paramref name="count"/> in-place on the copy.
    /// </summary>
    /// <param name="source">
    /// The single-dimensional array to copy. Must not be <see langword="null"/>,
    /// must have <see cref="Array.Rank"/> of 1, and must have been validated by the
    /// caller.
    /// </param>
    /// <param name="index">
    /// The zero-based index of the first element to reverse. Must be pre-validated.
    /// </param>
    /// <param name="count">
    /// The number of elements to reverse. Must be pre-validated.
    /// </param>
    /// <returns>
    /// A new <see cref="Array"/> of the same element type and length as
    /// <paramref name="source"/>, with the slice [<paramref name="index"/>,
    /// <paramref name="index"/> + <paramref name="count"/>) reversed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is the non-generic counterpart to <see cref="ReverseCore{T}"/>.
    /// Because the element type is not known at compile time, it cannot use
    /// <see cref="GC.AllocateUninitializedArray{T}"/> or
    /// <see cref="MemoryExtensions.Reverse{T}(Span{T})"/>. Instead it allocates via
    /// <see cref="Array.CreateInstance(Type, int)"/>, copies via <see cref="Array.Copy(Array, Array, int)"/>, and
    /// reverses in-place via <see cref="Array.Reverse(Array, int, int)"/>.
    /// </para>
    /// <para>
    /// This method is <see langword="internal"/> to allow direct testing via assemblies
    /// granted <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/>
    /// access. All public entry points validate arguments before calling this method;
    /// no bounds checking or null checking is performed here.
    /// </para>
    /// </remarks>
    internal static Array ReverseArrayCore(Array source, int index, int count)
    {
        Type? elementType = source.GetType().GetElementType();
        if (elementType is null) throw new InvalidOperationException("Array element type could not be resolved.");
        var result = Array.CreateInstance(elementType, source.Length);
        Array.Copy(source, result, source.Length);

        if (count > 1)
            Array.Reverse(result, index, count);

        return result;
    }

    /// <summary>
    /// Allocates a copy of <paramref name="source"/> and reverses the slice defined by
    /// <paramref name="index"/> and <paramref name="count"/> in-place on the copy.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="source">The span to copy. Must not be modified during the operation.</param>
    /// <param name="index">
    /// The zero-based index of the first element to reverse within the copied array.
    /// Must be a valid, pre-validated index into <paramref name="source"/>.
    /// </param>
    /// <param name="count">
    /// The number of elements to reverse. Must be a valid, pre-validated count relative
    /// to <paramref name="index"/> and <paramref name="source"/>.
    /// </param>
    /// <returns>
    /// A new <typeparamref name="T"/>[] containing all elements of <paramref name="source"/>,
    /// with the slice [<paramref name="index"/>, <paramref name="index"/> +
    /// <paramref name="count"/>) reversed. The caller receives a <see cref="Span{T}"/> or
    /// <typeparamref name="T"/>[] depending on which public overload initiated the call,
    /// both backed by this same allocation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns <typeparamref name="T"/>[] rather than <see cref="Span{T}"/> so that both
    /// the span-based and array-based public overloads can share this single allocation
    /// without a second heap allocation. <typeparamref name="T"/>[] implicitly converts to
    /// <see cref="Span{T}"/>, so span callers pay no extra cost.
    /// </para>
    /// <para>
    /// This method is <see langword="internal"/> so that it can be exercised directly by
    /// unit tests in assemblies granted access via
    /// <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/>, without
    /// exposing it as part of the public API surface. All public entry points validate their
    /// arguments before calling this method; no bounds checking is performed here.
    /// </para>
    /// <para>
    /// The full contents of <paramref name="source"/> are copied into a new heap-allocated
    /// array via <see cref="ReadOnlySpan{T}.CopyTo"/>, then the slice corresponding to
    /// [<paramref name="index"/>, <paramref name="index"/> + <paramref name="count"/>) is
    /// reversed in-place via <see cref="MemoryExtensions.Reverse{T}(Span{T})"/>. Both
    /// operations delegate to hardware-accelerated SIMD paths (AVX-512, AVX2, SSE) where
    /// available, falling back to a scalar swap loop for remainder elements or unsupported
    /// hardware.
    /// </para>
    /// <para>
    /// For unmanaged element types, zero-initialisation of the backing array is elided via
    /// <see cref="GC.AllocateUninitializedArray{T}"/>, since <see cref="ReadOnlySpan{T}.CopyTo"/>
    /// overwrites every element before the array is returned.
    /// </para>
    /// <para>
    /// The <c>count &gt; 1</c> guard avoids entering the SIMD dispatch machinery for
    /// degenerate slices (empty or single element), which would be wasted overhead.
    /// </para>
    /// </remarks>
    internal static T[] ReverseCore<T>(ReadOnlySpan<T> source, int index, int count)
    {
        T[] result = RuntimeHelpers.IsReferenceOrContainsReferences<T>()
            ? new T[source.Length]
            : GC.AllocateUninitializedArray<T>(source.Length);

        source.CopyTo(result);

        if (count > 1)
            MemoryExtensions.Reverse(((Span<T>)result).Slice(index, count));

        return result;
    }
}