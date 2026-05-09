// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.CanTransformMultipleBlocks.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305.CanTransformMultipleBlocks" /> is <see langword="true" />
    /// so the framework streams data through the algorithm in block-sized chunks.
    /// </summary>
    [TestMethod]
    public void CanTransformMultipleBlocks_ShouldBeTrue()
    {
        using var poly = new Poly1305();

        Assert.IsTrue(poly.CanTransformMultipleBlocks);
    }
}
