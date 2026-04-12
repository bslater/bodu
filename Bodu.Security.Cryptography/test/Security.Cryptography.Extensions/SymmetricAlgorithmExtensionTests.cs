// -----------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Bodu.Infrastructure;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    [TestClass]
    public partial class SymmetricAlgorithmExtensionTests
    {
        public static IEnumerable<object[]> SymmetricAlgorithmTestData()
        {
            yield return new object[] { SimpleReversingSymmetricAlgorithm.Create() };
            yield return new object[] { Aes.Create() };
        }

        private SymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
    }
}