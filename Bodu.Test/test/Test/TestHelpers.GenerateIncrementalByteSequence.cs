namespace Bodu.Test;

public static partial class TestHelpers
{
    /// <summary>
    /// Generates a deterministic sequence of incrementing byte values.
    /// </summary>
    /// <param name="start">
    /// The first byte value in the generated sequence.
    /// </param>
    /// <param name="count">
    /// The number of bytes to generate.
    /// </param>
    /// <returns>
    /// A byte array containing <paramref name="count" /> sequential byte values beginning at
    /// <paramref name="start" /> and incrementing by one for each subsequent element.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than zero.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The requested sequence would exceed the maximum value representable by <see cref="byte" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The generated sequence is deterministic and is commonly used for repeatable hashing,
    /// encoding, cryptographic, and known-answer test scenarios.
    /// </para>
    /// <para>
    /// For example, specifying <paramref name="start" /> as <c>0x00</c> and
    /// <paramref name="count" /> as <c>4</c> produces the sequence:
    /// <c>{ 0x00, 0x01, 0x02, 0x03 }</c>.
    /// </para>
    /// <para>
    /// The sequence cannot wrap beyond <see cref="byte.MaxValue" />. Attempting to generate
    /// a range that exceeds <c>0xFF</c> results in an <see cref="ArgumentException" />.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] bytes = GenerateIncrementalByteSequence(0x10, 4);
    /// // Produces: { 0x10, 0x11, 0x12, 0x13 }
    /// </code>
    /// </example>
    public static byte[] GenerateIncrementalByteSequence(byte start, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count > (byte.MaxValue - start + 1))
        {
            throw new ArgumentException(
                "The requested byte sequence exceeds the valid byte range.",
                nameof(count));
        }

        return Enumerable.Range(start, count)
                         .Select(static i => (byte)i)
                         .ToArray();
    }

    /// <summary>
    /// Generates a deterministic sequence of incrementing byte values, wrapping after <see cref="byte.MaxValue" />.
    /// </summary>
    /// <param name="start">
    /// The first byte value in the generated sequence.
    /// </param>
    /// <param name="count">
    /// The number of bytes to generate.
    /// </param>
    /// <returns>
    /// A byte array containing <paramref name="count" /> byte values beginning at <paramref name="start" /> and
    /// incrementing by one for each subsequent element, wrapping after <see cref="byte.MaxValue" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than zero.
    /// </exception>
    /// <remarks>
    /// This method is intended for deterministic test data where the requested length may exceed the number of
    /// distinct values representable by <see cref="byte" />. For non-wrapping sequences, use
    /// <see cref="GenerateIncrementalByteSequence(byte, int)" />.
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] bytes = GenerateRepeatingIncrementalByteSequence(0xFE, 4);
    /// // Produces: { 0xFE, 0xFF, 0x00, 0x01 }
    /// </code>
    /// </example>
    public static byte[] GenerateRepeatingIncrementalByteSequence(byte start, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        byte[] result = new byte[count];

        for (int i = 0; i < result.Length; i++)
            result[i] = unchecked((byte)(start + i));

        return result;
    }
}
