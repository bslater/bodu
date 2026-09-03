// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored 64-bit integer is returned.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenPresent_ShouldReturnValue()
    {
        MapiPropertyCollection collection = CreateSingle(0x0E08, MapiPropertyType.Int64, 123_456_789_012L);

        Assert.AreEqual(123_456_789_012L, collection.GetInt64(0x0E08));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />, and that a 32-bit value stored under the
    /// 64-bit probe is not converted.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenAbsentOrDifferentClrType_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetInt64(0x0E08));
        Assert.IsNull(CreateSingle(0x0E08, MapiPropertyType.Int64, 42).GetInt64(0x0E08));
    }

    /// <summary>
    /// Verifies that integers stored under the 32-bit and 16-bit types are widened by the 64-bit accessor, so a size
    /// a writer recorded as <c>PT_LONG</c> is not reported as absent.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenStoredAsNarrowerIntegerType_ShouldWiden()
    {
        Assert.AreEqual(42L, CreateSingle(0x0E08, MapiPropertyType.Int32, 42).GetInt64(0x0E08));
        Assert.AreEqual(7L, CreateSingle(0x0E08, MapiPropertyType.Int16, (short)7).GetInt64(0x0E08));
    }
}
