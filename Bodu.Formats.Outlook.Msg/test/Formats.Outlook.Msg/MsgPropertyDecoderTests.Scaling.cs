// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.Scaling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that decoding a storage with tens of thousands of variable-length properties completes within a
    /// budget only a linear stream lookup can meet: resolving each value stream by scanning every child of the
    /// storage is quadratic in the property count, and the property stream is bounded only by the container.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decode_WhenStorageHoldsManyVariableLengthProperties_ShouldCompleteWithinLinearBudget()
    {
        const int PropertyCount = 30_000;
        var builder = new MsgFixtureBuilder();
        for (int i = 0; i < PropertyCount; i++)
            builder.AddBinary((ushort)(0x0100 + i), [1]);

        using MemoryStream stream = builder.Build();
        using var compound = CompoundFile.Open(stream);

        var stopwatch = Stopwatch.StartNew();
        MapiPropertyCollection properties = MsgPropertyDecoder.Decode(
            compound.RootStorage, MsgPropertyStreamKind.Root, CompoundValidationLevel.Compatible, inheritedEncoding: null, out _);
        stopwatch.Stop();

        Assert.AreEqual(PropertyCount, properties.Count);
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Decoding {PropertyCount} properties took {stopwatch.Elapsed.TotalSeconds:F1} s — the value-stream lookup is not linear.");
    }
}
