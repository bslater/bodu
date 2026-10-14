// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that the message conveniences and scalar properties decode from the synthetic property context, with
    /// a <c>PT_STRING8</c> value decoded under the message's declared code page rather than the store fallback.
    /// </summary>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    public void Properties_WhenSyntheticStore_ShouldDecodeScalars(string testName, PstValidationLevel level)
    {
        _ = testName;

        using OutlookMailStore store = OpenSynthetic(level: level);

        OutlookMailMessage message = GetFullMessage(store);

        Assert.AreEqual(PstMessagingFixtureBuilder.SenderName, message.SenderName);
        Assert.AreEqual(PstMessagingFixtureBuilder.SenderEmailAddress, message.SenderEmailAddress);
        Assert.AreEqual("IPM.Note", message.MessageClass);
        Assert.AreEqual(true, message.Properties.GetBoolean(MapiPropertyIds.HasAttachments));
        Assert.AreEqual(
            PstMessagingFixtureBuilder.BodyText,
            message.Properties.GetString(MapiPropertyIds.Body),
            "The PT_STRING8 body must decode under the declared windows-1251 code page.");
    }

    /// <summary>
    /// Verifies that a variable-size multi-valued Unicode property decodes from the MS-PST count-plus-offset-table
    /// layout into its element strings.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMultiValuedUnicode_ShouldDecodeElements()
    {
        using OutlookMailStore store = OpenSynthetic();

        string[]? values = GetFullMessage(store).Properties
            .GetStringArray(PstMessagingFixtureBuilder.MvUnicodePropertyId);

        Assert.IsNotNull(values);
        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.MvUnicodeValues, values);
    }

    /// <summary>
    /// Verifies that a packed fixed-width multi-valued Int32 property decodes into its element values.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMultiValuedInt32_ShouldDecodeElements()
    {
        using OutlookMailStore store = OpenSynthetic();

        var tag = new MapiPropertyTag(((uint)PstMessagingFixtureBuilder.MvInt32PropertyId << 16) | 0x1003);
        Assert.IsTrue(GetFullMessage(store).Properties.TryGetValue(tag, out MapiProperty? property));
        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.MvInt32Values, (int[])property.Value!);
    }

    /// <summary>
    /// Verifies that a packed multi-valued <c>AppTime</c> property (<c>PtypMultipleFloatingTime</c>) decodes into its
    /// OLE Automation dates at every validation level, as a single-valued <c>AppTime</c> decodes to its value.
    /// </summary>
    /// <param name="testName">The validation level, by name.</param>
    /// <param name="level">The validation level the store is opened at.</param>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    public void Properties_WhenMultiValuedAppTime_ShouldDecodeElements(string testName, PstValidationLevel level)
    {
        double[] dates = [45000.5, -1.25];
        using OutlookMailStore store = OpenSynthetic(b => b.MvAppTimeValues = dates, level);

        var tag = new MapiPropertyTag(((uint)PstMessagingFixtureBuilder.MvAppTimePropertyId << 16) | 0x1007);
        Assert.IsTrue(GetFullMessage(store).Properties.TryGetValue(tag, out MapiProperty? property), testName);
        CollectionAssert.AreEqual(dates, (double[])property.Value!, testName);
    }

    /// <summary>
    /// Verifies that a store that omits unresolvable values leaves out a message property whose heap reference does not
    /// resolve, keeps the rest of the message readable, and reports the omission, at every validation level.
    /// </summary>
    /// <param name="testName">The row's display name.</param>
    /// <param name="level">The validation level.</param>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    public void Properties_WhenAValueDoesNotResolve_ForOmittingStore_ShouldOmitAndReportIt(string testName, PstValidationLevel level)
    {
        _ = testName;
        var reports = new List<OutlookOmittedValue>();
        var options = new OutlookMailStoreReaderOptions
        {
            ValidationLevel = level,
            OmitUnresolvableValues = true,
            ValueOmittedCallback = reports.Add,
        };

        using OutlookMailStore store = OpenSynthetic(static b => b.DanglingMessagePropertyIds.Add(MapiPropertyIds.SenderName), options);
        OutlookMailMessage message = GetFullMessage(store);

        Assert.IsNull(message.SenderName);
        Assert.AreEqual(PstMessagingFixtureBuilder.SenderEmailAddress, message.Properties.GetString(MapiPropertyIds.SenderEmailAddress));
        Assert.AreEqual(PstMessagingFixtureBuilder.BodyText, message.BodyText);

        OutlookOmittedValue report = reports.Single(static r => r.NodeId.Value == PstMessagingFixtureBuilder.MessageNodeId);
        Assert.AreEqual(MapiPropertyIds.SenderName, report.PropertyId);
        Assert.IsNull(report.RowId);
        Assert.AreEqual(OutlookOmittedValueReason.UnresolvableReference, report.Reason);
    }

    /// <summary>
    /// Verifies that a value the reader cannot decode is reported as <see cref="OutlookOmittedValueReason.UndecodableValue" />
    /// under the tolerant validation levels, whether or not the store omits unresolvable values.
    /// </summary>
    [TestMethod]
    public void Properties_WhenAValueIsUndecodable_ShouldReportItAsUndecodable()
    {
        var reports = new List<OutlookOmittedValue>();
        var options = new OutlookMailStoreReaderOptions { ValueOmittedCallback = reports.Add };

        // A multi-valued string whose count promises five values but whose payload holds no offset table.
        using OutlookMailStore store = OpenSynthetic(static b => b.ExtraMessageValues.Add((0x6003, 0x101F, [0x05, 0x00, 0x00, 0x00])), options);
        OutlookMailMessage message = GetFullMessage(store);

        Assert.IsFalse(message.Properties.Contains(new MapiPropertyTag(0x6003, (MapiPropertyType)0x101F)));

        OutlookOmittedValue report = reports.Single(static r => r.NodeId.Value == PstMessagingFixtureBuilder.MessageNodeId);
        Assert.AreEqual((ushort)0x6003, report.PropertyId);
        Assert.AreEqual(OutlookOmittedValueReason.UndecodableValue, report.Reason);
    }

    /// <summary>
    /// Verifies that a store with a callback but every value intact reports nothing.
    /// </summary>
    [TestMethod]
    public void Properties_WhenEveryValueDecodes_ShouldReportNothing()
    {
        var reports = new List<OutlookOmittedValue>();
        var options = new OutlookMailStoreReaderOptions { OmitUnresolvableValues = true, ValueOmittedCallback = reports.Add };

        using OutlookMailStore store = OpenSynthetic(configure: null, options);
        OutlookMailMessage message = GetFullMessage(store);
        _ = message.Properties;
        _ = message.Recipients;

        Assert.AreEqual(0, reports.Count);
    }

    /// <summary>
    /// Verifies that an exception the omission callback throws propagates from the property access.
    /// </summary>
    [TestMethod]
    public void Properties_WhenValueOmittedCallbackThrows_ShouldPropagateTheException()
    {
        var options = new OutlookMailStoreReaderOptions
        {
            OmitUnresolvableValues = true,
            ValueOmittedCallback = static _ => throw new InvalidOperationException("stop"),
        };

        using OutlookMailStore store = OpenSynthetic(static b => b.DanglingMessagePropertyIds.Add(MapiPropertyIds.SenderName), options);
        OutlookMailMessage message = store.RootFolder.EnumerateSubfolders().Single().EnumerateMessages()
            .Single(static m => m.Node.NodeId.Value == PstMessagingFixtureBuilder.MessageNodeId);

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = message.Properties;
        });

        Assert.AreEqual("stop", ex.Message);
    }

    /// <summary>
    /// Verifies that, by default, a message property whose heap reference does not resolve fails the message's
    /// properties with <see cref="OutlookPstFormatException" /> at every validation level, keeping the container's
    /// <see cref="PstFileFormatException" /> as the inner exception.
    /// </summary>
    /// <param name="testName">The row's display name.</param>
    /// <param name="level">The validation level.</param>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    [DataRow("Minimal", PstValidationLevel.Minimal)]
    public void Properties_WhenAValueDoesNotResolve_ShouldThrowOutlookPstFormatException(string testName, PstValidationLevel level)
    {
        _ = testName;
        using OutlookMailStore store = OpenSynthetic(static b => b.DanglingMessagePropertyIds.Add(MapiPropertyIds.SenderName), level);
        OutlookMailMessage message = store.RootFolder.EnumerateSubfolders().Single().EnumerateMessages()
            .Single(static m => m.Node.NodeId.Value == PstMessagingFixtureBuilder.MessageNodeId);

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = message.Properties;
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
        Assert.AreEqual(PstFileError.InvalidHeap, ((PstFileFormatException)ex.InnerException).Error);
    }

    /// <summary>
    /// Verifies that reading the properties of a message whose node has a corrupt heap throws
    /// <see cref="OutlookPstFormatException" />, keeping the container's <see cref="PstFileFormatException" /> as the
    /// inner exception.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMessageHeapIsCorrupt_ShouldThrowOutlookPstFormatException()
    {
        // 0x200024 is the Inbox's first message.
        byte[] input = OutlookMailStoreTests.GenerateWithCorruptHeap("gen:Mail()", 0x200024);
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(input, writable: false), new OutlookMailStoreReaderOptions());
        OutlookMailMessage message = store.RootFolder.EnumerateSubfolders().Single().EnumerateMessages().Single();

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = message.Properties;
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
        Assert.AreEqual(PstFileError.InvalidHeap, ((PstFileFormatException)ex.InnerException).Error);
    }

    /// <summary>
    /// Verifies that a container exception the omission callback itself throws propagates from the property access
    /// unchanged rather than as a container fault of the store.
    /// </summary>
    [TestMethod]
    public void Properties_WhenValueOmittedCallbackThrowsPstFileException_ShouldPropagateItUnchanged()
    {
        var thrown = new PstFileFormatException("thrown by the callback");
        var options = new OutlookMailStoreReaderOptions
        {
            OmitUnresolvableValues = true,
            ValueOmittedCallback = _ => throw thrown,
        };

        using OutlookMailStore store = OpenSynthetic(static b => b.DanglingMessagePropertyIds.Add(MapiPropertyIds.SenderName), options);
        OutlookMailMessage message = store.RootFolder.EnumerateSubfolders().Single().EnumerateMessages()
            .Single(static m => m.Node.NodeId.Value == PstMessagingFixtureBuilder.MessageNodeId);

        PstFileFormatException ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = message.Properties;
        });

        Assert.AreSame(thrown, ex);
    }
}
