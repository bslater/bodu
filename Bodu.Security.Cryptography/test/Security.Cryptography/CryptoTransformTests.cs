// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Base class for testing types implementing <see cref="System.Security.Cryptography.ICryptoTransform" />.
    /// </summary>
    /// <typeparam name="TCryptoTransform">The crypto transform type under test.</typeparam>
    public abstract partial class CryptoTransformTests<TCryptoTransform>
        where TCryptoTransform : System.Security.Cryptography.ICryptoTransform
    {
        /// <summary>
        /// Creates a new instance of the cryptographic transform under test.
        /// </summary>
        /// <returns>
        /// A newly constructed and fully initialised instance of <typeparamref name="TCryptoTransform" />, ready for use in test scenarios.
        /// </returns>
        protected abstract TCryptoTransform CreateAlgorithm();
    }
}
