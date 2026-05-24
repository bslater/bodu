// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.TotalLengthOverflow.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.Append(System.ReadOnlySpan{byte})" /> throws
    /// <see cref="InvalidOperationException" /> when the cumulative byte count would overflow
    /// <see cref="ulong.MaxValue" /> rather than silently wrapping <c>TotalLength</c> back through zero. Exercised
    /// via reflection on the private setter because reaching <c>ulong.MaxValue</c> through real appends would
    /// require 16 exabytes of input.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Append_WhenTotalLengthWouldOverflow_ShouldThrowInvalidOperationException()
    {
        OverflowableBlockHasher hasher = new();
        hasher.SeedTotalLength(ulong.MaxValue - 4UL);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            hasher.Append(new byte[8]);
        });
    }

    /// <summary>
    /// A test-only block hasher that exposes a setter for the base-class <c>TotalLength</c> via reflection so
    /// tests can drive it near <see cref="ulong.MaxValue" /> without appending exabytes of input.
    /// </summary>
    private sealed class OverflowableBlockHasher
        : BlockNonCryptographicHashAlgorithm<OverflowableBlockHasher>
    {

        private static readonly PropertyInfo s_totalLengthProperty =
            typeof(BlockNonCryptographicHashAlgorithm<OverflowableBlockHasher>)
                .GetProperty(
                    "TotalLength",
                    BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("BlockNonCryptographicHashAlgorithm<T>.TotalLength property is unavailable to reflection.");

        public OverflowableBlockHasher()
            : base(hashLengthInBytes: 4, blockSize: 4)
        {
        }

        public void SeedTotalLength(ulong value) =>
            s_totalLengthProperty.SetValue(this, value);

        protected override OverflowableBlockHasher Clone()
        {
            OverflowableBlockHasher clone = new();
            clone.CopyResidualStateFrom(this);
            return clone;
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) =>
            block.ToArray();

        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
        }

        protected override byte[] ProcessFinalBlock() => new byte[4];

        protected override bool ShouldPadFinalBlock() => false;

    }

}
