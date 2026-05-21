// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Base class for <see cref="EnumExtensions" /> tests. Declares the flag enumerations, of varying underlying
/// integer width, shared by the bitwise-helper test suites.
/// </summary>
[TestClass]
public partial class EnumExtensionsTests
{
    /// <summary>A flag enumeration backed by <see cref="byte" />, exercising the one-byte reinterpretation path.</summary>
    [Flags]
    internal enum ByteFlags : byte
    {
        /// <summary>No flag bits set.</summary>
        None = 0,

        /// <summary>The first flag bit.</summary>
        A = 1,

        /// <summary>The second flag bit.</summary>
        B = 1 << 1,

        /// <summary>The third flag bit.</summary>
        C = 1 << 2,

        /// <summary>The most significant bit of the underlying byte.</summary>
        High = 1 << 7,
    }

    /// <summary>A flag enumeration backed by the default <see cref="int" /> underlying type.</summary>
    [Flags]
    internal enum IntFlags
    {
        /// <summary>No flag bits set.</summary>
        None = 0,

        /// <summary>The first flag bit.</summary>
        A = 1,

        /// <summary>The second flag bit.</summary>
        B = 1 << 1,

        /// <summary>The third flag bit.</summary>
        C = 1 << 2,

        /// <summary>The fourth flag bit.</summary>
        D = 1 << 3,
    }

    /// <summary>A flag enumeration backed by <see cref="long" />, exercising the eight-byte reinterpretation path.</summary>
    [Flags]
    internal enum LongFlags : long
    {
        /// <summary>No flag bits set.</summary>
        None = 0L,

        /// <summary>The first flag bit.</summary>
        A = 1L,

        /// <summary>The second flag bit.</summary>
        B = 1L << 1,

        /// <summary>A flag bit set at position 40, beyond the 32-bit range.</summary>
        High = 1L << 40,
    }
}
