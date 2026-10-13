// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.SignedByteEnum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// A test enumeration over <see cref="sbyte" /> whose defined values run contiguously from -2 to 1.
    /// </summary>
    private enum SignedByteEnum : sbyte
    {
        /// <summary>The lowest value.</summary>
        MinusTwo = -2,

        /// <summary>The second value.</summary>
        MinusOne = -1,

        /// <summary>The third value.</summary>
        Zero = 0,

        /// <summary>The highest value.</summary>
        One = 1,
    }
}
