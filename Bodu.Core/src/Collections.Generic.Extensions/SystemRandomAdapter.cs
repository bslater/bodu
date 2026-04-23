// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SystemRandomAdapter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Adapts a <see cref="System.Random"/> instance to the <see cref="IRandomGenerator"/> interface.
/// </summary>
/// <remarks>
/// This adapter allows <see cref="System.Random"/> to be used wherever code depends on the <see cref="IRandomGenerator"/> abstraction,
/// enabling straightforward substitution for testing or alternative generators.
/// </remarks>
public sealed class SystemRandomAdapter :
    IRandomGenerator
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRandomAdapter"/> class with a default <see cref="System.Random"/> instance.
    /// </summary>
    /// <remarks>The <see cref="System.Random"/> default constructor is used, which seeds the generator from the system clock.</remarks>
    public SystemRandomAdapter()
        : this(new Random()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRandomAdapter"/> class with the specified <see cref="System.Random"/> instance.
    /// </summary>
    /// <param name="random">The <see cref="System.Random"/> instance to wrap. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="random"/> is <see langword="null"/>.</exception>
    public SystemRandomAdapter(Random random) =>
        _random = random ?? throw new ArgumentNullException(nameof(random));

    /// <inheritdoc />
    public int Next(int maxValue) => _random.Next(maxValue);
}
