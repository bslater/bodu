// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

[TestClass]
public partial class BufferConverterTests
{
    // =========================================================================
    // Shared fixtures and helpers
    // =========================================================================

    /// <summary>
    /// A 16-byte buffer of ascending byte values, usable as source for reading up to four 32-bit integers or two 64-bit values.
    /// </summary>
    internal static byte[] AscendingBytes => new byte[]
    {
        0x00, 0x01, 0x02, 0x03,
        0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B,
        0x0C, 0x0D, 0x0E, 0x0F,
    };
}
