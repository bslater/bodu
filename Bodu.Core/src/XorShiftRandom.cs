// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu;

/// <summary>
/// Represents a high-performance, non-cryptographic pseudo-random number generator based on Marsaglia's xorshift128
/// algorithm.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="XorShiftRandom" /> derives each output by combining four 32-bit state words through three xor-and-shift
/// operations — a generator class introduced by George Marsaglia in 2003. The cost per draw is a handful of register
/// operations with no branching, no division, and no memory allocation, making it materially faster than
/// <see cref="System.Random" /> in tight inner loops on every supported runtime.
/// </para>
/// <para>
/// The type subclasses <see cref="System.Random" /> so it can be passed anywhere <see cref="System.Random" /> is
/// accepted, and it also implements <see cref="IRandomGenerator" /> for use with the library's shuffle and sampling
/// helpers. The default constructor seeds from <see cref="Environment.TickCount" />; the seeded constructors are the
/// preferred choice in tests and reproducible benchmarks.
/// </para>
/// <para>
/// Instances are not thread-safe — state updates are non-atomic and concurrent draws will corrupt the internal state.
/// Use a per-thread instance, an external lock, or a thread-local pool when sharing across threads is required.
/// </para>
/// <para>
/// The algorithm produces a uniform distribution over the 32-bit output range and has a period of <c>2^128 − 1</c>. It
/// is <em>not</em> suitable for cryptographic use, secret material, or any context where an attacker can observe
/// outputs and recover state. Use <see cref="System.Security.Cryptography.RandomNumberGenerator" /> for those cases.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Reproducible shuffle and sample in a test.
/// var rng = new XorShiftRandom(seed: 1234);
/// int    roll  = rng.Next(6) + 1;           // value in [1, 6]
/// double angle = rng.NextDouble() * Math.Tau;
///
/// var bag = new[] { "A", "B", "C", "D" };
/// ShuffleHelpers.Shuffle(bag, rng);
///]]>
/// </example>
public sealed class XorShiftRandom :
    System.Random,
    IRandomGenerator
{
    private uint _x;
    private uint _y;
    private uint _z;
    private uint _w;

    /// <summary>
    /// Initializes a new instance of the <see cref="XorShiftRandom" /> class using a system-generated seed.
    /// </summary>
    /// <remarks>
    /// The default seed is derived from <see cref="Environment.TickCount" /> at the time of construction.
    /// </remarks>
    public XorShiftRandom()
        : this((uint)Environment.TickCount)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="XorShiftRandom" /> class with a 32-bit signed integer seed.
    /// </summary>
    /// <param name="seed">The seed used to initialize the random generator.</param>
    public XorShiftRandom(int seed)
        : this((uint)seed)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="XorShiftRandom" /> class with a 32-bit unsigned seed.
    /// </summary>
    /// <param name="seed">The seed used to initialize the random generator.</param>
    public XorShiftRandom(uint seed)
    {
        // Initialize four internal states with XOR-variations of the seed for better distribution
        _x = seed;
        _y = seed ^ 0x6C8E9CF5U;
        _z = seed ^ 0x94D049BBU;
        _w = seed ^ 0x5A17D7F9U;
    }

    /// <inheritdoc />
    public override int Next() => Next(int.MaxValue);

    /// <inheritdoc />
    public override int Next(int maxValue)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(maxValue, 0);
        return (int)(NextUInt32() % (uint)maxValue);
    }

    /// <inheritdoc />
    public override int Next(int minValue, int maxValue)
    {
        ThrowHelper.ThrowIfGreaterThanOrEqualOther(minValue, maxValue);
        var range = (uint)(maxValue - minValue);
        return minValue + (int)(NextUInt32() % range);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void NextBytes(byte[] buffer)
    {
        ThrowHelper.ThrowIfNull(buffer);

        for (var i = 0; i < buffer.Length; i++)
        {
            if ((i & 3) == 0)
            {
                var rnd = NextUInt32();
                buffer[i++] = (byte)(rnd & 0xFF);
                if (i < buffer.Length) buffer[i++] = (byte)((rnd >> 8) & 0xFF);
                if (i < buffer.Length) buffer[i++] = (byte)((rnd >> 16) & 0xFF);
                if (i < buffer.Length) buffer[i++] = (byte)((rnd >> 24) & 0xFF);
                i--; // account for loop increment
            }
        }
    }

    /// <inheritdoc />
    public override double NextDouble() => NextUInt32() / (double)uint.MaxValue;

    /// <summary>
    /// Generates the next 32-bit random number using XOR-shift algorithm.
    /// </summary>
    /// <returns>A pseudo-random <see cref="uint" /> drawn from the current generator state.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        var t = _x ^ (_x << 11);
        _x = _y;
        _y = _z;
        _z = _w;
        _w ^= (_w >> 19) ^ t ^ (t >> 8);
        return _w;
    }
}
