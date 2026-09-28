// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.LinearTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that the linear transform matches the reference implementation.
    /// </summary>
    [TestMethod]
    public void LinearTransform_ShouldMatchTheReference()
    {
        foreach (uint[] input in BitslicedInputs)
        {
            uint x0 = input[0], x1 = input[1], x2 = input[2], x3 = input[3];
            uint e0 = input[0], e1 = input[1], e2 = input[2], e3 = input[3];

            SerpentCore.LinearTransform(ref x0, ref x1, ref x2, ref x3);
            SerpentReference.LinearTransform(ref e0, ref e1, ref e2, ref e3);

            CollectionAssert.AreEqual(new[] { e0, e1, e2, e3 }, new[] { x0, x1, x2, x3 });
        }
    }
}
