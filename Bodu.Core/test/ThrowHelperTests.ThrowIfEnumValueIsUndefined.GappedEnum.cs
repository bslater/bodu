// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.GappedEnum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// A test enumeration whose defined values, -2, 0 and 5, leave gaps between them.
    /// </summary>
    private enum GappedEnum
    {
        /// <summary>The lowest value.</summary>
        Low = -2,

        /// <summary>The middle value.</summary>
        Zero = 0,

        /// <summary>The highest value.</summary>
        High = 5,
    }
}
