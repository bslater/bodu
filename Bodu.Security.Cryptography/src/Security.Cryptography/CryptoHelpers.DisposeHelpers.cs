namespace Bodu.Security.Cryptography
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

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
        /// <param name="array">The array whose contents will be securely zeroed. If <see langword="null" />, the call is a no-op.</param>
        /// <remarks>
        /// Unlike <see cref="ClearAndNullify{T}(ref T[])" />, this overload does not nullify the caller's reference. It is useful for
        /// clearing the contents of readonly fields or shared buffers whose reference must remain valid.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(T[] array) where T : unmanaged
        {
            if (array is null) return;
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(array.AsSpan()));
        }

        /// <summary>
        /// Securely zeroes the contents of an unmanaged array and sets the caller's reference to <see langword="null" />.
        /// </summary>
        /// <typeparam name="T">The element type of the array. Must be an unmanaged value type.</typeparam>
        /// <param name="array">A reference to the array to clear and nullify. If <see langword="null" />, the call is a no-op.</param>
        /// <remarks>
        /// The contents are cleared using <see cref="CryptographicOperations.ZeroMemory" /> before the reference is set to
        /// <see langword="null" />. This helper is intended for releasing sensitive data such as key material or intermediate
        /// hash state.
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