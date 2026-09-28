// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.InverseLinearTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that the inverse linear transform matches the reference implementation and undoes
    /// the linear transform.
    /// </summary>
    [TestMethod]
    public void InverseLinearTransform_ShouldMatchTheReferenceAndUndoTheTransform()
    {
        foreach (uint[] input in BitslicedInputs)
        {
            uint x0 = input[0], x1 = input[1], x2 = input[2], x3 = input[3];
            uint e0 = input[0], e1 = input[1], e2 = input[2], e3 = input[3];

            SerpentCore.InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            SerpentReference.InverseLinearTransform(ref e0, ref e1, ref e2, ref e3);
            CollectionAssert.AreEqual(new[] { e0, e1, e2, e3 }, new[] { x0, x1, x2, x3 }, "against the reference");

            SerpentCore.LinearTransform(ref x0, ref x1, ref x2, ref x3);
            CollectionAssert.AreEqual(input, new[] { x0, x1, x2, x3 }, "the transform after its inverse");
        }
    }
}
