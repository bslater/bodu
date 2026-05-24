// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Overflow.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Append" /> throws <see cref="OverflowException" /> when
    /// the internal element count is already at <see cref="int.MaxValue" />, rather than silently producing a
    /// negative minimum and tripping a misleading downstream failure inside the growth path.
    /// </summary>
    [TestMethod]
    public void Append_WhenCountAtIntMaxValue_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            builder.Append(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})" /> throws
    /// <see cref="OverflowException" /> when <c>_count + source.Length</c> would overflow.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenCountPlusSpanLengthOverflows_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            builder.AppendRange(new byte[] { 1, 2 }.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany" /> throws <see cref="OverflowException" />
    /// when <c>_count + count</c> would overflow.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenCountPlusValueCountOverflows_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            builder.AddMany(0, 5);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetSpan" /> throws <see cref="OverflowException" />
    /// when <c>_count + sizeHint</c> would overflow.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenCountPlusSizeHintOverflows_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = builder.GetSpan(sizeHint: 1024);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetMemory" /> throws <see cref="OverflowException" />
    /// when <c>_count + sizeHint</c> would overflow.
    /// </summary>
    [TestMethod]
    public void GetMemory_WhenCountPlusSizeHintOverflows_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = builder.GetMemory(sizeHint: 1024);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})" />
    /// throws <see cref="OverflowException" /> on the <see cref="System.Collections.Generic.ICollection{T}" />
    /// fast path when <c>_count + col.Count</c> would overflow.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenCollectionFastPathCountPlusOverflows_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        IEnumerable<byte> source = new List<byte> { 1, 2, 3 };

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            builder.AppendRange(source);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})" />
    /// throws <see cref="OverflowException" /> on the lazy <see cref="System.Collections.Generic.IEnumerable{T}" />
    /// foreach path when <c>_count + 1</c> would overflow at the first iteration.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenLazyEnumerableForeachOverflows_ShouldThrowOverflowException()
    {
        using var builder = new PooledBufferBuilder<byte>(initialCapacity: 1);

        SetCount(builder, int.MaxValue);

        IEnumerable<byte> source = LazyBytes();

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            builder.AppendRange(source);
        });

        static IEnumerable<byte> LazyBytes()
        {
            yield return 0;
        }
    }

    /// <summary>
    /// Sets the private <c>_count</c> field via reflection so that overflow boundaries can be exercised
    /// without allocating multi-gigabyte arrays. The builder is left in a deliberately inconsistent state
    /// for the duration of the call; only <see cref="PooledBufferBuilder{T}.Dispose" /> is safe afterwards.
    /// </summary>
    private static void SetCount<T>(PooledBufferBuilder<T> builder, int value) =>
        typeof(PooledBufferBuilder<T>)
            .GetField("_count", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(builder, value);
}
