// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Gumm.Tables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class Gumm
{
    /// <summary>
    /// The standard Cayley table of the dihedral group <i>D</i><sub>5</sub> (order 10), where <c>s_d[a, b]</c> is the
    /// index of <c>a * b</c>.
    /// </summary>
    /// <remarks>
    /// Elements are numbered 0 to 9: 0 to 4 are the rotations <c>(1, x)</c> for <c>x = 0..4</c> and 5 to 9 are the
    /// reflections <c>(-1, x)</c> for <c>x = 0..4</c>. The group operation is
    /// <c>(e1, x1) * (e2, x2) = (e1 e2, e1 x2 + x1)</c> with the inner arithmetic taken modulo five.
    /// </remarks>
    private static readonly byte[,] s_d = new byte[10, 10]
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 },
        { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 },
        { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 },
        { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 },
        { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
        { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 },
        { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 },
        { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 },
        { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
    };

    /// <summary>
    /// The multiplicative inverse table in the dihedral group <i>D</i><sub>5</sub>: <c>s_inv[x]</c> is the element
    /// <c>y</c> such that <c>s_d[x, y]</c> is zero.
    /// </summary>
    private static readonly byte[] s_inv = [0, 4, 3, 2, 1, 5, 6, 7, 8, 9];

    /// <summary>
    /// Gumm's transform <c>T</c> applied at odd positions, where <c>s_t[n]</c> is the index of <c>T</c> applied to
    /// element <c>n</c>.
    /// </summary>
    /// <remarks>
    /// <c>T(e, x) = (e, e(a - x) + b)</c> with <c>a = 2</c> and <c>b = 1</c>. <c>T</c> is a permutation of <i>D</i><sub>5</sub>,
    /// and because <c>a</c> and <c>b</c> are both nonzero modulo five it satisfies the anti-symmetry property under
    /// which <c>T(u) * v = T(v) * u</c> (and <c>u * T(v) = v * T(u)</c>) implies <c>u = v</c> — the property that
    /// guarantees detection of every adjacent transposition.
    /// </remarks>
    private static readonly byte[] s_t = [3, 2, 1, 0, 4, 9, 5, 6, 7, 8];
}
