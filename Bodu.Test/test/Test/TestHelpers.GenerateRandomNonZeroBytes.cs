namespace Bodu.Test;

public static partial class TestHelpers
{
    /// <summary>
    /// Creates a byte array filled with cryptographically strong non-zero random bytes.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>
    /// A byte array of length <paramref name="length" /> containing only non-zero random values.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="length" /> is less than zero.
    /// </exception>
    public static byte[] GenerateRandomNonZeroBytes(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

        byte[] buffer = new byte[length];

        if (length == 0)
            return buffer;

        System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);

        Span<byte> value = stackalloc byte[1];
        for (int i = 0; i < buffer.Length; i++)
        {
            while (buffer[i] == 0)
            {
                System.Security.Cryptography.RandomNumberGenerator.Fill(value);
                buffer[i] = value[0];
            }
        }

        return buffer;
    }
}
