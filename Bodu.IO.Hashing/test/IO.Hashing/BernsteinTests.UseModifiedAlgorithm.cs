// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.UseModifiedAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

public partial class BernsteinTests
{
    /// <summary>
    /// Verifies that toggling <see cref="Bernstein.UseModifiedAlgorithm" /> before any input has been consumed
    /// switches the active mixing form.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenSetBeforeHashing_ShouldAffectResult()
    {
        var input = NonCryptographicHashSharedInputs.Abc;

        Bernstein original = new();
        Bernstein modified = new() { UseModifiedAlgorithm = true };
        original.Append(input);
        modified.Append(input);

        CollectionAssert.AreNotEqual(original.GetCurrentHash(), modified.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that setting <see cref="Bernstein.UseModifiedAlgorithm" /> after input has been consumed
    /// throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenSetAfterHashingStarted_ShouldThrow()
    {
        Bernstein algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.UseModifiedAlgorithm = true);
    }
}
