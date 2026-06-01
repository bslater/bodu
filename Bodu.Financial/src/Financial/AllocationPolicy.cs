// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies the algorithm a <see cref="MonetaryContext" /> uses to distribute residual minor units when allocating a
/// monetary amount across shares.
/// </summary>
public enum AllocationPolicy
{
    /// <summary>
    /// Distribute residual minor units by the largest-remainder (Hamilton) method, breaking ties by ascending index.
    /// </summary>
    LargestRemainder = 0,
}
