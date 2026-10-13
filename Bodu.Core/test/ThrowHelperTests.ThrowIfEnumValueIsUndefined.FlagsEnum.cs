// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.FlagsEnum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// A test <see cref="FlagsAttribute" /> enumeration whose named values are 0, 1, 2 and 4.
    /// </summary>
    [Flags]
    private enum FlagsEnum
    {
        /// <summary>No flag.</summary>
        None = 0,

        /// <summary>The first flag.</summary>
        Read = 1,

        /// <summary>The second flag.</summary>
        Write = 2,

        /// <summary>The third flag.</summary>
        Execute = 4,
    }
}
