// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.UnsignedLongEnum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// A test enumeration over <see cref="ulong" /> whose two defined values are the largest it can hold.
    /// </summary>
    private enum UnsignedLongEnum : ulong
    {
        /// <summary>The lower value.</summary>
        Big = ulong.MaxValue - 1,

        /// <summary>The higher value, the largest an unsigned 64-bit integer holds.</summary>
        Biggest = ulong.MaxValue,
    }
}
