using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides unit tests for symmetric algorithms to verify encryption, decryption, and property behaviors.
    /// </summary>
    /// <typeparam name="T">The type of symmetric algorithm under test.</typeparam>
    [TestClass]
    public abstract partial class SymmetricAlgorithmTests<T>
        where T : System.Security.Cryptography.SymmetricAlgorithm
    {
        /// <summary>
        /// Creates an instance of the symmetric algorithm under test.
        /// </summary>
        /// <returns>An instance of the symmetric algorithm.</returns>
        protected abstract T CreateAlgorithm();
    }
}