// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStreamNames.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Provides the MS-OXMSG storage and stream naming conventions: the fixed names and the formatters that compose
/// tag-derived and index-derived names.
/// </summary>
internal static class MsgStreamNames
{
    /// <summary>The name of the property stream present in every message, recipient, and attachment storage.</summary>
    internal const string PropertiesStreamName = "__properties_version1.0";

    /// <summary>The name of the named-property mapping storage under the root.</summary>
    internal const string NameIdStorageName = "__nameid_version1.0";

    /// <summary>The name of the embedded-message storage inside an attachment storage.</summary>
    internal const string EmbeddedMessageStorageName = "__substg1.0_3701000D";

    /// <summary>The name prefix of recipient storages; a zero-based hexadecimal index follows.</summary>
    internal const string RecipientStoragePrefix = "__recip_version1.0_#";

    /// <summary>The name prefix of attachment storages; a zero-based hexadecimal index follows.</summary>
    internal const string AttachmentStoragePrefix = "__attach_version1.0_#";

    /// <summary>The name prefix of property value streams; the eight-digit tag follows.</summary>
    private const string SubstgStreamPrefix = "__substg1.0_";

    /// <summary>
    /// Composes the value-stream name for a variable-length property tag.
    /// </summary>
    /// <param name="tagValue">The raw 32-bit property tag.</param>
    /// <returns>The stream name, for example <c>__substg1.0_0037001F</c>.</returns>
    internal static string GetSubstgStreamName(uint tagValue) =>
        SubstgStreamPrefix + tagValue.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Composes the per-element value-stream name for a multi-valued variable-length property.
    /// </summary>
    /// <param name="tagValue">The raw 32-bit property tag, including the multi-valued flag.</param>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The element stream name, for example <c>__substg1.0_0037101F-00000000</c>.</returns>
    internal static string GetMultiValueElementStreamName(uint tagValue, int index) =>
        GetSubstgStreamName(tagValue) + "-" + index.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Composes a recipient storage name.
    /// </summary>
    /// <param name="index">The zero-based recipient index.</param>
    /// <returns>The storage name, for example <c>__recip_version1.0_#00000000</c>.</returns>
    internal static string GetRecipientStorageName(int index) =>
        RecipientStoragePrefix + index.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Composes an attachment storage name.
    /// </summary>
    /// <param name="index">The zero-based attachment index.</param>
    /// <returns>The storage name, for example <c>__attach_version1.0_#00000000</c>.</returns>
    internal static string GetAttachmentStorageName(int index) =>
        AttachmentStoragePrefix + index.ToString("X8", CultureInfo.InvariantCulture);
}
