// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.CanReuseTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305.CanReuseTransform" /> is <see langword="false" /> as
    /// documented in RFC 8439, signalling to consumers that the algorithm must not be reused
    /// across multiple messages with the same key.
    /// </summary>
    [TestMethod]
    public void CanReuseTransform_ShouldBeFalse()
    {
        using var poly = new Poly1305();

        Assert.IsFalse(poly.CanReuseTransform,
            "Poly1305 is a one-time authenticator and must report CanReuseTransform = false.");
    }
}
