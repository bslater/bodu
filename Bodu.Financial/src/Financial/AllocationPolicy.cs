// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies the algorithm for distributing residual minor units when allocating a monetary amount across shares.
/// </summary>
/// <remarks>
/// <see cref="MonetaryContext.Allocation" /> carries this value, but no allocation member consults a context yet: the
/// <c>Allocate</c> methods always use <see cref="LargestRemainder" />.
/// </remarks>
public enum AllocationPolicy
{
    /// <summary>
    /// Distribute residual minor units by the largest-remainder (Hamilton) method, breaking ties by ascending index.
    /// </summary>
    LargestRemainder = 0,
}
