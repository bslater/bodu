// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Base class for testing types implementing <see cref="ICryptoTransform" />.
    /// </summary>
    /// <typeparam name="TAlgorithm">The crypto transform type under test.</typeparam>
    public abstract partial class CryptoTransformTests<TAlgorithm>
        where TAlgorithm : System.Security.Cryptography.ICryptoTransform
    {
        /// <summary>
        /// Creates a new instance of the cryptographic hash algorithm under test.
        /// </summary>
        /// <returns>
        /// A newly constructed and fully initialized instance of <typeparamref name="TAlgorithm" />, ready for use in hash test scenarios.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is used as the factory method for producing clean instances of the algorithm under test. Implementations may
        /// configure the instance with default settings, keys, or parameters as appropriate for the test context.
        /// </para>
        /// </remarks>
        protected abstract TAlgorithm CreateAlgorithm();

        /// <summary>
        /// Gets the expected hash result when computing the hash of an empty byte array using the default algorithm variant.
        /// </summary>
        /// <value>A non-null <see cref="byte" /> array containing the expected hash value for an empty input.</value>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the expected hash for the "Empty" input is not defined in the test vector data for the default variant.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This property is used in unit tests to validate that the algorithm produces the correct and consistent result when hashing an
        /// empty input sequence. The value must match the reference hash from an authoritative source (e.g., test vectors from RFCs or
        /// specification suites).
        /// </para>
        /// </remarks>
        protected abstract byte[] ExpectedEmptyInputHash { get; }
    }
}