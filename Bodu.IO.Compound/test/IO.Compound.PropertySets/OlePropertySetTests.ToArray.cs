// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertySetTests.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class OlePropertySetTests
{
    /// <summary>
    /// Verifies that a <c>VT_FILETIME</c> whose top bit is set is written back by <see cref="OlePropertySet.ToArray" />
    /// with the same 64 bits it was read with.
    /// </summary>
    /// <remarks>
    /// The value, 9319997877838640000, is the FILETIME 920355314183864 milliseconds after 1970, which a reader that
    /// splits it into signed halves misreads.
    /// </remarks>
    [TestMethod]
    public void ToArray_WhenFileTimeHasItsTopBitSet_ShouldKeepItsValue()
    {
        const ulong FileTime = 9319997877838640000;
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (11, PropertySetBytes.Typed(0x0040, PropertySetBytes.Int64(unchecked((long)FileTime)))));

        OlePropertySet written = OlePropertySet.Parse(OlePropertySet.Parse(bytes).ToArray());

        Assert.AreEqual(unchecked((long)FileTime), written[11]!.Value);
    }
}
