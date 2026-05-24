// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherCtorGuardTests.TestFletcher.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class FletcherTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Test-only <see cref="Fletcher{TSelf}" /> subclass used solely to reach the protected constructor with
    /// arbitrary hash sizes. The parameterless public constructor satisfies the CRTP <c>new()</c> constraint and
    /// is never invoked directly by tests.
    /// </summary>
    private sealed class TestFletcher
        : Fletcher<TestFletcher>
    {

        public TestFletcher()
            : base(16)
        {
        }

        public TestFletcher(int hashSize)
            : base(hashSize)
        {
        }

    }

}
