// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.TestEnum.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

/// <summary>
/// Sample enum whose only defined values are <see cref="A" /> (0) and <see cref="B" /> (1). Used by the
/// <c>ThrowIfEnumValueIsUndefined</c> test cases to drive the defined/undefined validation branches.
/// </summary>
public enum TestEnum
{
    A = 0,
    B = 1,
}
