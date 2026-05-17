// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.GetBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Returns a byte array representing the binary encoding of the specified numeric value, in the requested byte
    /// order.
    /// </summary>
    /// <typeparam name="T">
    /// The numeric type to convert. Supported types are <see cref="byte" />, <see cref="sbyte" />, <see cref="short" />
    /// , <see cref="ushort" />, <see cref="int" />, <see cref="uint" />, <see cref="long" />, <see cref="ulong" />,
    /// <see cref="float" />, and <see cref="double" />.
    /// </typeparam>
    /// <param name="value">The numeric value to convert.</param>
    /// <param name="asBigEndian">
    /// <see langword="true" /> to return bytes in big-endian order (most significant byte first);
    /// <see langword="false" /> to return bytes in little-endian order (least significant byte first). Defaults to
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// A byte array containing the binary representation of <paramref name="value" /> in the specified byte order.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T" /> is not a supported numeric type.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <see cref="BitConverter" /> is used to obtain the binary representation of each value in the system's native
    /// byte order. The resulting array is reversed when the requested byte order differs from the platform's native
    /// endianness.
    /// </para>
    /// <para>
    /// For <see cref="float" /> and <see cref="double" />, the encoding follows the IEEE 754 standard.
    /// </para>
    /// </remarks>
    public static byte[] GetBytes<T>(this T value, bool asBigEndian = false)
    {
        var bytes = value switch
        {
            byte b => new[] { b },
            sbyte sb => new[] { unchecked((byte)sb) },
            short s => BitConverter.GetBytes(s),
            ushort us => BitConverter.GetBytes(us),
            int i => BitConverter.GetBytes(i),
            uint u => BitConverter.GetBytes(u),
            long l => BitConverter.GetBytes(l),
            ulong ul => BitConverter.GetBytes(ul),
            float f => BitConverter.GetBytes(f),
            double d => BitConverter.GetBytes(d),
            _ => throw new InvalidOperationException($"Type '{typeof(T).Name}' is not a supported numeric type.")
        };

        // BitConverter always produces bytes in the system's native byte order. Reverse when the
        // requested endianness differs from the platform's native endianness.
        if (asBigEndian == BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return bytes;
    }
}
