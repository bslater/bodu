// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.DisposeHelpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class CryptoHelpers
{
    /// <summary>
    /// Securely zeroes the contents of a <see cref="Memory{T}" /> buffer using
    /// <see cref="CryptographicOperations.ZeroMemory" />.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged.</typeparam>
    /// <param name="memory">The memory buffer to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(Memory<T> memory)
        where T : unmanaged
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(memory.Span));
    }

    /// <summary>
    /// Securely zeroes the contents of a <see cref="Span{T}" /> using <see cref="CryptographicOperations.ZeroMemory" />
    /// .
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged.</typeparam>
    /// <param name="span">The span to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(Span<T> span)
        where T : unmanaged
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(span));
    }

    /// <summary>
    /// Securely zeroes the contents of an unmanaged array using <see cref="CryptographicOperations.ZeroMemory" />.
    /// </summary>
    /// <param name="array">
    /// The array whose contents will be securely zeroed. If <see langword="null" />, the call is a no-op.
    /// </param>
    /// <remarks>
    /// Unlike <see cref="ClearAndNullify{T}(ref T[])" />, this overload does not nullify the caller's reference. It is
    /// useful for clearing the contents of readonly fields or shared buffers whose reference must remain valid.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(byte[]? array)
    {
        if (array is null) return;
        CryptographicOperations.ZeroMemory(array);
    }

    /// <summary>
    /// Securely zeroes the contents of an unmanaged array using <see cref="CryptographicOperations.ZeroMemory" />.
    /// </summary>
    /// <typeparam name="T">The element type of the array. Must be unmanaged.</typeparam>
    /// <param name="array">
    /// The array whose contents will be securely zeroed. If <see langword="null" />, the call is a no-op.
    /// </param>
    /// <remarks>
    /// Unlike <see cref="ClearAndNullify{T}(ref T[])" />, this overload does not nullify the caller's reference. It is
    /// useful for clearing the contents of readonly fields or shared buffers whose reference must remain valid.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(T[] array)
        where T : unmanaged
    {
        if (array is null) return;
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(array.AsSpan()));
    }

    /// <summary>
    /// Securely zeroes the contents of an unmanaged array and sets the caller's reference to <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The element type of the array. Must be an unmanaged value type.</typeparam>
    /// <param name="array">
    /// A reference to the array to clear and nullify. If <see langword="null" />, the call is a no-op.
    /// </param>
    /// <remarks>
    /// The contents are cleared using <see cref="CryptographicOperations.ZeroMemory" /> before the reference is set to
    /// <see langword="null" />. This helper is intended for releasing sensitive data such as key material or
    /// intermediate hash state.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearAndNullify<T>(ref T[]? array)
        where T : unmanaged
    {
        if (array is null) return;

        // ZeroMemory already overwrites every byte with 0; the subsequent Array.Clear
        // would be a redundant second pass over the same buffer.
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(array.AsSpan()));
        array = null;
    }

    /// <summary>
    /// Securely zeroes the bytes of an unmanaged value in place.
    /// </summary>
    /// <typeparam name="T">The unmanaged value type to clear.</typeparam>
    /// <param name="value">A reference to the value whose contents will be overwritten with zeroes.</param>
    /// <remarks>
    /// <para>
    /// This method treats the supplied value as a contiguous sequence of bytes and overwrites those bytes using
    /// <see cref="CryptographicOperations.ZeroMemory" />.
    /// </para>
    /// <para>
    /// This is useful for clearing stack-allocated values, struct fields, and other unmanaged state that may contain
    /// sensitive material, such as cipher state, counters, tweak words, hash chaining values, authentication
    /// accumulators, or expanded-key fragments.
    /// </para>
    /// <para>
    /// Only the storage location referenced by <paramref name="value" /> is cleared. This method cannot clear previous
    /// copies that may have existed in registers, temporary locals, copied structs, arrays, native buffers, crash
    /// dumps, or other runtime-managed locations.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(ref T value)
        where T : unmanaged
    {
        CryptographicOperations.ZeroMemory(
            MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));
    }

    /// <summary>
    /// Securely clears the contents of a <see cref="MemoryStream" /> by overwriting its underlying buffer with zeros,
    /// resetting its length, and disposing the stream.
    /// </summary>
    /// <param name="stream">The <see cref="MemoryStream" /> whose buffer is to be cleared.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The supplied <see cref="MemoryStream" /> does not expose its underlying buffer and therefore cannot be securely
    /// cleared by this method.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method attempts to clear the entire underlying buffer, not just the active portion of the stream, because
    /// sensitive data may remain in unused capacity beyond the current <see cref="MemoryStream.Length" />.
    /// </para>
    /// <para>
    /// This method does not guarantee that all historical copies of the data have been removed from memory. Additional
    /// copies may exist if the stream was resized, if <see cref="MemoryStream.ToArray" /> was called, or if the data
    /// was converted into other managed objects such as strings.
    /// </para>
    /// <para>
    /// For sensitive cryptographic material, prefer directly managed byte buffers where possible so they can be cleared
    /// with <see cref="CryptographicOperations.ZeroMemory(System.Span{byte})" /> without relying on
    /// <see cref="MemoryStream" />.
    /// </para>
    /// </remarks>
    public static void ClearAndNullify(MemoryStream stream)
    {
        ArraySegment<byte> segment = CryptoHelpers.GetBufferOrThrowIfInaccessible(stream);

        if (segment.Array is null)
            throw new InvalidOperationException(
                CryptoResourceStrings.Op_Invalid_MemoryStreamBufferInaccessible);

        CryptographicOperations.ZeroMemory(segment.Array.AsSpan(0, segment.Array.Length));

        stream.Position = 0;
        stream.SetLength(0);
        stream.Dispose();
    }
}
