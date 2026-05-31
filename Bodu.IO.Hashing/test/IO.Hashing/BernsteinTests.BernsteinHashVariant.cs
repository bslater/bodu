// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.BernsteinHashVariant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Identifies the Bernstein djb2 algorithm variants exercised by <see cref="BernsteinTests" />: the classic
/// multiplicative form and the XOR-based modified form.
/// </summary>
public enum BernsteinHashVariant
{
    /// <summary>The original djb2 form <c>hash = (hash * 33) + c</c>.</summary>
    Default,

    /// <summary>The XOR-modified form <c>hash = (hash * 33) ^ c</c>.</summary>
    Modified,
}
