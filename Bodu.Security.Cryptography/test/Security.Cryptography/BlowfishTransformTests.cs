// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TCryptoTransform}" /> base tests
    /// against the <see cref="BlowfishTransform" /> implementation.
    /// </summary>
    [TestClass]
    internal sealed class BlowfishTransformTests
        : BlockCipherTransformTests<BlowfishTransform>
    {
        /// <inheritdoc />
        protected override BlowfishTransform CreateAlgorithm()
        {
            var algorithm = new Blowfish();
            algorithm.GenerateKey();
            algorithm.GenerateIV();
            return (BlowfishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
        }
    }
}
