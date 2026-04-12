using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public static partial class CryptoHelpers
    {
        /// <summary>
        /// Securely zeroes the contents of a <see cref="Memory{T}" /> buffer using <see cref="CryptographicOperations.ZeroMemory" />.
        /// </summary>
        /// <typeparam name="T">The element type. Must be unmanaged.</typeparam>
        /// <param name="memory">The memory buffer to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(Memory<T> memory) where T : unmanaged
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(memory.Span));
        }

        /// <summary>
        /// Securely zeroes the contents of a <see cref="Span{T}" /> using <see cref="CryptographicOperations.ZeroMemory" />.
        /// </summary>
        /// <typeparam name="T">The element type. Must be unmanaged.</typeparam>
        /// <param name="span">The span to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(Span<T> span) where T : unmanaged
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(span));
        }

        /// <summary>
        /// Securely zeroes the contents of an unmanaged array using <see cref="CryptographicOperations.ZeroMemory" />.
        /// </summary>
        /// <typeparam name="T">The element type of the array. Must be unmanaged.</typeparam>
        /// <param name="array">The array whose contents will be securely zeroed.</param>
        /// <remarks>
        /// Unlike <see cref="ClearAndNullify{T}" />, this overload does not nullify the array reference. Useful for clearing contents of
        /// readonly fields or shared buffers.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(T[] array) where T : unmanaged
        {
            if (array is null) return;
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(array.AsSpan()));
        }

        /// <summary>
        /// Securely zeroes and nullifies an array of unmanaged values such as <see cref="byte" />, <see cref="uint" />, or <see cref="ulong" />.
        /// </summary>
        /// <typeparam name="T">The element type of the array. Must be an unmanaged value type.</typeparam>
        /// <param name="array">The array to securely clear and nullify.</param>
        /// <remarks>
        /// The method zeroes the memory using <see cref="CryptographicOperations.ZeroMemory" /> and sets the reference to <see langword="null" />. This
        /// is useful for clearing sensitive data such as key material or intermediate hash state.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearAndNullify<T>(ref T[]? array) where T : unmanaged
        {
            if (array is null) return;

            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(array.AsSpan()));
            Array.Clear(array, 0, array.Length);
            array = null;
        }
    }
}