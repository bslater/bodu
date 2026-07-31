// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyIds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Provides curated well-known MAPI property identifiers (the <c>PidTag*</c> constants of MS-OXPROPS).
/// </summary>
/// <remarks>
/// This is a deliberately small subset — the identifiers the Outlook readers' convenience surfaces and common consumer
/// scenarios need. Every property of a message remains reachable through <see cref="MapiPropertyCollection" /> by raw
/// identifier regardless of whether it is listed here.
/// </remarks>
public static class MapiPropertyIds
{
    /// <summary>The message class (<c>PidTagMessageClass</c>, <c>0x001A</c>).</summary>
    public const ushort MessageClass = 0x001A;

    /// <summary>The message importance (<c>PidTagImportance</c>, <c>0x0017</c>).</summary>
    public const ushort Importance = 0x0017;

    /// <summary>The message sensitivity (<c>PidTagSensitivity</c>, <c>0x0036</c>).</summary>
    public const ushort Sensitivity = 0x0036;

    /// <summary>The message subject (<c>PidTagSubject</c>, <c>0x0037</c>).</summary>
    public const ushort Subject = 0x0037;

    /// <summary>The client submit time (<c>PidTagClientSubmitTime</c>, <c>0x0039</c>).</summary>
    public const ushort ClientSubmitTime = 0x0039;

    /// <summary>The transport message headers (<c>PidTagTransportMessageHeaders</c>, <c>0x007D</c>).</summary>
    public const ushort TransportMessageHeaders = 0x007D;

    /// <summary>The display names of the blind-carbon-copy recipients (<c>PidTagDisplayBcc</c>, <c>0x0E02</c>).</summary>
    public const ushort DisplayBcc = 0x0E02;

    /// <summary>The display names of the carbon-copy recipients (<c>PidTagDisplayCc</c>, <c>0x0E03</c>).</summary>
    public const ushort DisplayCc = 0x0E03;

    /// <summary>The display names of the primary recipients (<c>PidTagDisplayTo</c>, <c>0x0E04</c>).</summary>
    public const ushort DisplayTo = 0x0E04;

    /// <summary>The message delivery time (<c>PidTagMessageDeliveryTime</c>, <c>0x0E06</c>).</summary>
    public const ushort MessageDeliveryTime = 0x0E06;

    /// <summary>The attachment size in bytes (<c>PidTagAttachSize</c>, <c>0x0E20</c>).</summary>
    public const ushort AttachSize = 0x0E20;

    /// <summary>The plain-text message body (<c>PidTagBody</c>, <c>0x1000</c>).</summary>
    public const ushort Body = 0x1000;

    /// <summary>The compressed RTF message body (<c>PidTagRtfCompressed</c>, <c>0x1009</c>).</summary>
    public const ushort RtfCompressed = 0x1009;

    /// <summary>The HTML message body (<c>PidTagHtml</c>, <c>0x1013</c>).</summary>
    public const ushort Html = 0x1013;

    /// <summary>The internet message identifier (<c>PidTagInternetMessageId</c>, <c>0x1035</c>).</summary>
    public const ushort InternetMessageId = 0x1035;

    /// <summary>The sender display name (<c>PidTagSenderName</c>, <c>0x0C1A</c>).</summary>
    public const ushort SenderName = 0x0C1A;

    /// <summary>The recipient type (<c>PidTagRecipientType</c>, <c>0x0C15</c>).</summary>
    public const ushort RecipientType = 0x0C15;

    /// <summary>The sender email address (<c>PidTagSenderEmailAddress</c>, <c>0x0C1F</c>).</summary>
    public const ushort SenderEmailAddress = 0x0C1F;

    /// <summary>The display name of a recipient or attachment (<c>PidTagDisplayName</c>, <c>0x3001</c>).</summary>
    public const ushort DisplayName = 0x3001;

    /// <summary>The address type of a recipient (<c>PidTagAddressType</c>, <c>0x3002</c>).</summary>
    public const ushort AddressType = 0x3002;

    /// <summary>The email address of a recipient (<c>PidTagEmailAddress</c>, <c>0x3003</c>).</summary>
    public const ushort EmailAddress = 0x3003;

    /// <summary>The store support mask (<c>PidTagStoreSupportMask</c>, <c>0x340D</c>).</summary>
    public const ushort StoreSupportMask = 0x340D;

    /// <summary>The attachment payload (<c>PidTagAttachDataBinary</c> / <c>PidTagAttachDataObject</c>, <c>0x3701</c>).</summary>
    public const ushort AttachData = 0x3701;

    /// <summary>The attachment file extension (<c>PidTagAttachExtension</c>, <c>0x3703</c>).</summary>
    public const ushort AttachExtension = 0x3703;

    /// <summary>The 8.3 attachment file name (<c>PidTagAttachFilename</c>, <c>0x3704</c>).</summary>
    public const ushort AttachFilename = 0x3704;

    /// <summary>The attachment method (<c>PidTagAttachMethod</c>, <c>0x3705</c>).</summary>
    public const ushort AttachMethod = 0x3705;

    /// <summary>The long attachment file name (<c>PidTagAttachLongFilename</c>, <c>0x3707</c>).</summary>
    public const ushort AttachLongFilename = 0x3707;

    /// <summary>The attachment content identifier (<c>PidTagAttachContentId</c>, <c>0x3712</c>).</summary>
    public const ushort AttachContentId = 0x3712;

    /// <summary>The internet code page of the message (<c>PidTagInternetCodepage</c>, <c>0x3FDE</c>).</summary>
    public const ushort InternetCodepage = 0x3FDE;

    /// <summary>The code page used for string properties (<c>PidTagMessageCodepage</c>, <c>0x3FFD</c>).</summary>
    public const ushort MessageCodepage = 0x3FFD;
}
