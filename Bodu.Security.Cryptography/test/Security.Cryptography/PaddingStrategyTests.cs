// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a reusable base class for verifying <see cref="IPaddingStrategy" />
    /// implementations. Concrete test classes supply algorithm-specific parameters via the
    /// protected abstract members below.
    /// </summary>
    [TestClass]
    public abstract partial class PaddingStrategyTests<TPadding>
        where TPadding : IPaddingStrategy, new()
    {
        /// <summary>
        /// Creates a new instance of the padding strategy under test.
        /// </summary>
        protected virtual TPadding CreatePadding() => new TPadding();

        /// <summary>
        /// Gets the block size in bytes to use when exercising the Pad/Unpad API.
        /// </summary>
        protected abstract int BlockSize { get; }

        /// <summary>
        /// Gets a value indicating whether <c>Unpad</c> may reject corrupted padding with
        /// <see cref="CryptographicException" />. Strategies that do not validate padding
        /// (e.g. <c>NoPadding</c>) return <see langword="false" />.
        /// </summary>
        protected abstract bool ValidatesPaddingOnUnpad { get; }

        /// <summary>
        /// Gets a sample plaintext that is shorter than <see cref="BlockSize" /> by the
        /// specified <paramref name="residualBytes" /> count. Used by the single-byte padding
        /// and round-trip tests.
        /// </summary>
        protected abstract byte[] CreatePlaintextWithResidual(int residualBytes);
    }
}
