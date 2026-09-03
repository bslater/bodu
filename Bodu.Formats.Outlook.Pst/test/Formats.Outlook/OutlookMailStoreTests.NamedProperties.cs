// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.NamedProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that a numeric named entry resolves through the GUID stream to its property-set identifier and
    /// numeric name, in both directions.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenNumericNamedEntry_ShouldResolveIdentity()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        var tag = new MapiPropertyTag(PstMessagingFixtureBuilder.NamedNumericPropertyId, MapiPropertyType.Unicode);
        Assert.IsTrue(store.TryGetPropertyName(tag, out MapiNamedProperty name));
        Assert.AreEqual(PstMessagingFixtureBuilder.NamedPropertySetId, name.PropertySetId);
        Assert.AreEqual(PstMessagingFixtureBuilder.NamedNumericId, name.Id);
        Assert.IsNull(name.Name);

        Assert.IsTrue(store.TryGetNamedPropertyId(name, out ushort id));
        Assert.AreEqual(PstMessagingFixtureBuilder.NamedNumericPropertyId, id);
    }

    /// <summary>
    /// Verifies that a string named entry resolves through the string stream to its <c>PS_PUBLIC_STRINGS</c>-scoped
    /// name, in both directions.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenStringNamedEntry_ShouldResolveIdentity()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        var tag = new MapiPropertyTag(PstMessagingFixtureBuilder.NamedStringPropertyId, MapiPropertyType.Unicode);
        Assert.IsTrue(store.TryGetPropertyName(tag, out MapiNamedProperty name));
        Assert.AreEqual(PstMessagingFixtureBuilder.PublicStringsPropertySetId, name.PropertySetId);
        Assert.AreEqual(PstMessagingFixtureBuilder.NamedStringName, name.Name);
        Assert.IsNull(name.Id);

        Assert.IsTrue(store.TryGetNamedPropertyId(
            new MapiNamedProperty(PstMessagingFixtureBuilder.PublicStringsPropertySetId, PstMessagingFixtureBuilder.NamedStringName),
            out ushort id));
        Assert.AreEqual(PstMessagingFixtureBuilder.NamedStringPropertyId, id);
    }

    /// <summary>
    /// Verifies that an identity the map does not carry, and a tag outside the mapped range, both resolve to nothing.
    /// </summary>
    [TestMethod]
    public void TryGetNamedPropertyId_WhenIdentityUnmapped_ShouldReturnFalse()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        Assert.IsFalse(store.TryGetNamedPropertyId(
            new MapiNamedProperty(PstMessagingFixtureBuilder.NamedPropertySetId, 0x1234u), out _));
        Assert.IsFalse(store.TryGetPropertyName(
            new MapiPropertyTag(0x9000, MapiPropertyType.Unicode), out _));
    }

    /// <summary>
    /// Verifies that a store without the name-to-id map node resolves nothing rather than failing.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenNameToIdMapAbsent_ShouldReturnFalse()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.IncludeNameToIdMap = false);

        Assert.IsFalse(store.TryGetPropertyName(
            new MapiPropertyTag(PstMessagingFixtureBuilder.NamedNumericPropertyId, MapiPropertyType.Unicode), out _));
    }

    /// <summary>
    /// Verifies that a truncated entry stream yields an empty mapping under the tolerant levels.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenEntryStreamTruncated_ShouldReturnFalse()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.TruncateNameMapEntryStream = true);

        Assert.IsFalse(store.TryGetPropertyName(
            new MapiPropertyTag(PstMessagingFixtureBuilder.NamedNumericPropertyId, MapiPropertyType.Unicode), out _));
    }

    /// <summary>
    /// Verifies that a truncated entry stream throws under strict validation.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenEntryStreamTruncated_ForStrictValidation_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.TruncateNameMapEntryStream = true,
            PstValidationLevel.Strict);

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = store.TryGetPropertyName(
                new MapiPropertyTag(PstMessagingFixtureBuilder.NamedNumericPropertyId, MapiPropertyType.Unicode), out _);
        });
    }

    /// <summary>
    /// Verifies that a string stream naming a whitespace-only property is treated as malformed content: skipped under
    /// the tolerant levels rather than escaping as an argument exception.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenStringNameIsWhitespace_ShouldReturnFalse()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.NameMapStringStreamOverride = BuildWhitespaceNameStream());

        Assert.IsFalse(store.TryGetPropertyName(
            new MapiPropertyTag(PstMessagingFixtureBuilder.NamedStringPropertyId, MapiPropertyType.Unicode), out _));
    }

    /// <summary>
    /// Verifies that a string stream naming a whitespace-only property throws the format exception under strict
    /// validation, never <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyName_WhenStringNameIsWhitespace_ForStrictValidation_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.NameMapStringStreamOverride = BuildWhitespaceNameStream(),
            PstValidationLevel.Strict);

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = store.TryGetPropertyName(
                new MapiPropertyTag(PstMessagingFixtureBuilder.NamedStringPropertyId, MapiPropertyType.Unicode), out _);
        });
    }

    /// <summary>
    /// Builds a name-map string stream whose only entry is two spaces.
    /// </summary>
    /// <returns>The stream bytes.</returns>
    private static byte[] BuildWhitespaceNameStream()
    {
        byte[] name = System.Text.Encoding.Unicode.GetBytes("  ");
        var stream = new byte[4 + name.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(stream, name.Length);
        name.CopyTo(stream, 4);
        return stream;
    }

    /// <summary>
    /// Verifies that the reference fixture's name-to-id map parses under strict validation: its first entry is the
    /// numeric name <c>0x8205</c> mapped to identifier <c>0x8000</c>, the mapping round-trips, and the map carries at
    /// least one string named entry.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void TryGetPropertyName_WhenReferenceFixture_ShouldParseNameToIdMap()
    {
        using OutlookMailStore store = OpenSample1(PstValidationLevel.Strict);

        Assert.IsTrue(store.TryGetPropertyName(new MapiPropertyTag(0x8000, MapiPropertyType.Unicode), out MapiNamedProperty first));
        Assert.AreEqual(0x8205u, first.Id, "The first NAMEID entry of sample1.pst is the numeric name 0x8205.");
        Assert.IsTrue(store.TryGetNamedPropertyId(first, out ushort roundTripped));
        Assert.AreEqual((ushort)0x8000, roundTripped);

        bool anyStringNamed = false;
        for (ushort id = 0x8000; id < 0x8100; id++)
        {
            if (store.TryGetPropertyName(new MapiPropertyTag(id, MapiPropertyType.Unicode), out MapiNamedProperty name)
                && name.Name is not null)
            {
                anyStringNamed = true;
                break;
            }
        }

        Assert.IsTrue(anyStringNamed, "The fixture's populated string stream implies at least one string named entry.");
    }
}
