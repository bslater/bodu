// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.BernsteinHashVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the Bernstein DJB2 algorithm variants exercised by <see cref="BernsteinTests" />:
/// the classic multiplicative form and the XOR-based modified form.
/// </summary>
public enum BernsteinHashVariant
{
    Default,
    Modified
}
