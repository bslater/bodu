// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

/// <summary>
/// Drives <see cref="BufferWriteKat" /> and <see cref="BufferCapacityKat" /> rows against
/// <see cref="PooledBufferBuilder{T}" />. Each KAT row asserts one observable outcome on a fresh
/// builder. Bespoke append, sort, and dispose coverage lives in the existing
/// <c>PooledBufferBuilderTests.*</c> per-member partials.
/// </summary>
[TestClass]
public sealed class PooledBufferBuilderKatTests
{
    private static IReadOnlyList<BufferWriteKat> WriteKats { get; } =
    [
        new(Name: "single item",       InitialCapacity: 16, Append: [42],                  ExpectedCount: 1),
        new(Name: "three items",       InitialCapacity: 16, Append: [1, 2, 3],             ExpectedCount: 3),
        new(Name: "fills capacity",    InitialCapacity: 4,  Append: [10, 20, 30, 40],      ExpectedCount: 4),
    ];

    // ArrayPool may round up the rented buffer size, so the "ExpectsGrowth = false" rows are framed
    // around the actual rented capacity rather than the requested initialCapacity.
    private static IReadOnlyList<BufferCapacityKat> CapacityKats { get; } =
    [
        new(Name: "small append into 256 builder",   InitialCapacity: 256,  AppendCount: 4,    ExpectsGrowth: false),
        new(Name: "append exceeds 256 capacity",     InitialCapacity: 256,  AppendCount: 1024, ExpectsGrowth: true),
    ];

    /// <summary>
    /// Verifies that each <see cref="BufferWriteKat" /> row leaves the builder with the expected
    /// element count after appending its payload.
    /// </summary>
    [TestMethod]
    public void Append_WhenKatPayloadAppended_ShouldYieldExpectedCount()
    {
        foreach (BufferWriteKat kat in WriteKats)
        {
            using PooledBufferBuilder<int> builder = new(kat.InitialCapacity);
            builder.AppendRange(kat.Append.AsSpan());

            Assert.AreEqual(
                kat.ExpectedCount,
                builder.WrittenCount,
                $"Buffer write KAT '{kat.Name}': count differs from expected.");
        }
    }

    /// <summary>
    /// Verifies that each <see cref="BufferCapacityKat" /> row produces the expected capacity-growth
    /// outcome: rows tagged <see cref="BufferCapacityKat.ExpectsGrowth" /> must end with capacity
    /// strictly greater than <see cref="BufferCapacityKat.InitialCapacity" />, while rows tagged
    /// <c>false</c> must leave capacity unchanged.
    /// </summary>
    [TestMethod]
    public void Append_WhenKatCapacityScenario_ShouldRespectGrowthExpectation()
    {
        foreach (BufferCapacityKat kat in CapacityKats)
        {
            using PooledBufferBuilder<int> builder = new(kat.InitialCapacity);
            var initialCapacity = builder.Capacity;

            for (var i = 0; i < kat.AppendCount; i++)
                builder.Append(i);

            if (kat.ExpectsGrowth)
            {
                Assert.IsGreaterThan(
                    initialCapacity,
                    builder.Capacity,
                    $"Buffer capacity KAT '{kat.Name}': expected growth past {initialCapacity}.");
            }
            else
            {
                Assert.AreEqual(
                    initialCapacity,
                    builder.Capacity,
                    $"Buffer capacity KAT '{kat.Name}': capacity must not grow.");
            }
        }
    }
}
