// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.ByteEnum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// A test enumeration over <see cref="byte" /> whose defined values run contiguously from 253 to 255.
    /// </summary>
    private enum ByteEnum : byte
    {
        /// <summary>The lowest value.</summary>
        Low = 253,

        /// <summary>The middle value.</summary>
        Middle = 254,

        /// <summary>The highest value, the largest a byte holds.</summary>
        High = 255,
    }
}
