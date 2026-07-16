// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherCtorGuardTests.TestFletcher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class FletcherTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Test-only <see cref="Fletcher" /> subclass used solely to reach the protected constructor with arbitrary hash
    /// sizes. It is never invoked through the snapshot path, so <see cref="CreateEmpty" /> returns a default instance.
    /// </summary>
    private sealed class TestFletcher
        : Fletcher
    {

        public TestFletcher()
            : base(16)
        {
        }

        public TestFletcher(int hashSize)
            : base(hashSize)
        {
        }

        protected override Fletcher CreateEmpty() =>
            new TestFletcher();

    }

}
