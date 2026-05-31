// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305.AlgorithmName" /> returns the literal <c>"Poly1305"</c> on a fresh
    /// instance — Poly1305 has a fixed 128-bit tag width with no version sub-typing.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenInstanceIsFresh_ShouldReturnLiteralPoly1305()
    {
        using var algorithm = new Poly1305 { Key = Poly1305TestKey };

        Assert.AreEqual("Poly1305", algorithm.AlgorithmName);
    }
}
