// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTypeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies that the <see cref="MapiPropertyType" /> codes are pinned to their MS-OXCDATA wire values.
/// </summary>
[TestClass]
public class MapiPropertyTypeTests
{
    /// <summary>
    /// Verifies that each enumeration member carries its published <c>PT_*</c> wire value.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="type">The enumeration member.</param>
    /// <param name="expected">The published wire value.</param>
    [TestMethod]
    [DataRow("Unspecified", MapiPropertyType.Unspecified, (ushort)0x0000)]
    [DataRow("Null", MapiPropertyType.Null, (ushort)0x0001)]
    [DataRow("Int16", MapiPropertyType.Int16, (ushort)0x0002)]
    [DataRow("Int32", MapiPropertyType.Int32, (ushort)0x0003)]
    [DataRow("Float", MapiPropertyType.Float, (ushort)0x0004)]
    [DataRow("Double", MapiPropertyType.Double, (ushort)0x0005)]
    [DataRow("Currency", MapiPropertyType.Currency, (ushort)0x0006)]
    [DataRow("AppTime", MapiPropertyType.AppTime, (ushort)0x0007)]
    [DataRow("ErrorCode", MapiPropertyType.ErrorCode, (ushort)0x000A)]
    [DataRow("Boolean", MapiPropertyType.Boolean, (ushort)0x000B)]
    [DataRow("Object", MapiPropertyType.Object, (ushort)0x000D)]
    [DataRow("Int64", MapiPropertyType.Int64, (ushort)0x0014)]
    [DataRow("String8", MapiPropertyType.String8, (ushort)0x001E)]
    [DataRow("Unicode", MapiPropertyType.Unicode, (ushort)0x001F)]
    [DataRow("SystemTime", MapiPropertyType.SystemTime, (ushort)0x0040)]
    [DataRow("Guid", MapiPropertyType.Guid, (ushort)0x0048)]
    [DataRow("Binary", MapiPropertyType.Binary, (ushort)0x0102)]
    public void Members_WhenComparedToWireValue_ShouldMatch(string testName, MapiPropertyType type, ushort expected)
    {
        _ = testName;

        Assert.AreEqual(expected, (ushort)type);
    }
}
