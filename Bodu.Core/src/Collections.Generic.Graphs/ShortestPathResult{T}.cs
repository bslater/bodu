// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShortestPathResult{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Represents the outcome of a shortest-path search: whether the target was reachable, the total distance, and the
/// reconstructed path.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <param name="Found">
/// <see langword="true" /> if a path to the target exists; otherwise, <see langword="false" />.
/// </param>
/// <param name="Distance">
/// The total weight of the path when <paramref name="Found" /> is <see langword="true" />; otherwise,
/// <see cref="double.PositiveInfinity" />.
/// </param>
/// <param name="Path">
/// The vertices of the path from source to target inclusive when <paramref name="Found" /> is <see langword="true" />;
/// otherwise, an empty list.
/// </param>
public readonly record struct ShortestPathResult<TVertex>(bool Found, double Distance, IReadOnlyList<TVertex> Path)
    where TVertex : notnull;
