// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.NamedProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>The PS_PUBLIC_STRINGS property-set GUID.</summary>
    private static readonly Guid s_publicStrings = new("00020329-0000-0000-C000-000000000046");

    /// <summary>
    /// Builds a message whose name-id storage maps the string name "Keywords" to identifier 0x8000.
    /// </summary>
    /// <returns>The fixture builder.</returns>
    private static MsgFixtureBuilder CreateWithNamedProperty() =>
        MsgFixtureBuilder.CreateMinimal()
            .WithNameId(
                Array.Empty<byte>(),
                MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 0),
                MsgFixtureBuilder.NameIdString("Keywords"))
            .AddMultiValueUnicode(0x8000, "urgent", "finance");

    /// <summary>
    /// Verifies that a named property resolves in both directions and addresses its decoded value.
    /// </summary>
    [TestMethod]
    public void TryGetNamedPropertyId_WhenNameMapped_ShouldResolveAndAddressValue()
    {
        using MemoryStream container = CreateWithNamedProperty().Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.IsTrue(message.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, "Keywords"), out ushort id));
        Assert.AreEqual((ushort)0x8000, id);
        CollectionAssert.AreEqual(new[] { "urgent", "finance" }, message.Properties.GetStringArray(id));

        Assert.IsTrue(message.TryGetPropertyName(new MapiPropertyTag(0x8000101Fu), out MapiNamedProperty name));
        Assert.AreEqual(new MapiNamedProperty(s_publicStrings, "Keywords"), name);
    }

    /// <summary>
    /// Verifies that an unmapped name reports absent, and that a message without a mapping storage resolves nothing.
    /// </summary>
    [TestMethod]
    public void TryGetNamedPropertyId_WhenNameUnmapped_ShouldReturnFalse()
    {
        using MemoryStream container = CreateMinimalContainer();

        using var message = OutlookMessage.OpenRead(container);

        Assert.IsFalse(message.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, "Keywords"), out _));
        Assert.IsFalse(message.TryGetPropertyName(new MapiPropertyTag(0x8000101Fu), out _));
    }

    /// <summary>
    /// Verifies that a nested message resolves named properties through the root's mapping.
    /// </summary>
    [TestMethod]
    public void TryGetNamedPropertyId_WhenNestedMessage_ShouldResolveThroughRootMapping()
    {
        using MemoryStream container = CreateWithNamedProperty()
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded.AddUnicode(MapiPropertyIds.Subject, "Inner")))
            .Build();

        using var message = OutlookMessage.OpenRead(container);
        OutlookMessage nested = message.Attachments[0].OpenMessage();

        Assert.IsTrue(nested.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, "Keywords"), out ushort id));
        Assert.AreEqual((ushort)0x8000, id);
    }

    /// <summary>
    /// Verifies that a numeric and a string name mapped in the same property set each resolve to the identifier of their
    /// own entry: <c>0x8000</c> for the first entry (LID <c>0x8501</c>) and <c>0x8001</c> for the second
    /// (<c>Keywords</c>).
    /// </summary>
    [TestMethod]
    public void TryGetNamedPropertyId_WhenNameIsMapped_ShouldReturnItsId()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddFixedEntry(0x80000003, 1)
            .AddFixedEntry(0x80010003, 2)
            .WithNameId(
                Array.Empty<byte>(),
                [.. MsgFixtureBuilder.NameIdEntry(0x8501, 2, isString: false, 0), .. MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 1)],
                MsgFixtureBuilder.NameIdString("Keywords"))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.IsTrue(message.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, "Keywords"), out ushort byName));
        Assert.AreEqual((ushort)0x8001, byName);
        Assert.IsTrue(message.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, 0x8501u), out ushort byLid));
        Assert.AreEqual((ushort)0x8000, byLid);
    }

    /// <summary>
    /// Verifies that a string name is matched ordinally: <c>keywords</c> does not resolve a property mapped as
    /// <c>Keywords</c>.
    /// </summary>
    [TestMethod]
    public void TryGetNamedPropertyId_WhenNameDiffersInCase_ShouldReturnFalse()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddFixedEntry(0x80000003, 1)
            .WithNameId(
                Array.Empty<byte>(),
                MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 0),
                MsgFixtureBuilder.NameIdString("Keywords"))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.IsFalse(message.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, "keywords"), out _));
    }
}
