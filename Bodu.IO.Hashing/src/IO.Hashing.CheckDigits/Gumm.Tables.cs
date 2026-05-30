// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Gumm.Tables.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class Gumm
{
    // Dihedral group D5 (order 10). Elements are numbered 0..9: 0..4 are the rotations (1, x) for x = 0..4 and
    // 5..9 are the reflections (-1, x) for x = 0..4. The group operation is (e1, x1) * (e2, x2) = (e1 e2, e1 x2 + x1)
    // with the inner arithmetic taken modulo 5; s_d[a, b] is the index of a * b. This is the standard D5 Cayley table.
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

    // Multiplicative inverse in D5: s_inv[x] is the element y such that s_d[x, y] = 0.
    private static readonly byte[] s_inv = [0, 4, 3, 2, 1, 5, 6, 7, 8, 9];

    // Gumm's transform T applied at odd positions: T(e, x) = (e, e(a - x) + b) with a = 2 and b = 1. s_t[n] is the
    // index of T applied to element n. T is a permutation of D5 and, because a and b are both nonzero modulo 5, it
    // satisfies the anti-symmetry property T(u) * v = T(v) * u (and u * T(v) = v * T(u)) implies u = v, which is what
    // guarantees detection of every adjacent transposition.
    private static readonly byte[] s_t = [3, 2, 1, 0, 4, 9, 5, 6, 7, 8];
}
