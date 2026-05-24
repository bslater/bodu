// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.SmallInputs.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> round-trips inputs of
    /// a representative range of sizes. The range straddles the stackalloc/pool threshold so a regression in the
    /// branching logic of either code path is caught.
    /// </summary>
    /// <param name="byteCount">The decoded byte count under test.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(192)]   // ~256 chars when encoded — straddles 256-char stackalloc threshold below
    [DataRow(256)]
    [DataRow(257)]
    [DataRow(512)]
    [DataRow(1024)]
    public void Decode_AcrossStackallocBoundary_ShouldMatchBclResult(int byteCount)
    {
        byte[] source = new byte[byteCount];
        new Random(byteCount).NextBytes(source);

        string encoded = Convert.ToBase64String(source);
        byte[] decoded = Base64.Decode(encoded.AsSpan());

        CollectionAssert.AreEqual(source, decoded);
    }
}
