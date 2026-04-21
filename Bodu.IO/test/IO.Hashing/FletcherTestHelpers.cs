// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTestHelpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing
{
    internal static class FletcherTestHelpers
    {
        public const string QuickBrownFox = "The quick brown fox jumps over the lazy dog";

        public static byte[] IncrementingBytes(int length)
        {
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
                data[i] = (byte)i;
            return data;
        }

        public static string ToHex(ReadOnlySpan<byte> bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            const string lookup = "0123456789ABCDEF";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = lookup[bytes[i] >> 4];
                chars[(i * 2) + 1] = lookup[bytes[i] & 0xF];
            }
            return new string(chars);
        }

        public static byte[] Utf8(string value) => System.Text.Encoding.UTF8.GetBytes(value);
    }
}
