// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegateHashAlgorithmFactoryTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class DelegateHashAlgorithmFactoryTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="DelegateHashAlgorithmFactory{T}" /> with a <see langword="null" /> builder
    /// delegate throws <see cref="ArgumentNullException" /> with the expected parameter name.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DelegateHashAlgorithmFactory<MD5>(null!);
        });

        Assert.AreEqual("builder", ex.ParamName);
    }
}
