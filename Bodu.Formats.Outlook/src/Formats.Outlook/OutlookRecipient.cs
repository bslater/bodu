// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookRecipient.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents one recipient of a message: a typed view over the recipient's decoded properties.
/// </summary>
/// <remarks>
/// The conveniences return <see langword="null" /> when the underlying property is absent; every recipient property
/// remains reachable through <see cref="Properties" />. The format readers construct instances from whichever
/// container structure carries the recipient — a <c>.msg</c> recipient storage or a PST recipient-table row — so the
/// type itself is container-free.
/// </remarks>
public sealed class OutlookRecipient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookRecipient" /> class.
    /// </summary>
    /// <param name="properties">The recipient's decoded properties. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="properties" /> is <see langword="null" />.</exception>
    public OutlookRecipient(MapiPropertyCollection properties)
    {
        ThrowHelper.ThrowIfNull(properties);

        Properties = properties;
    }

    /// <summary>
    /// Gets every decoded property of the recipient.
    /// </summary>
    /// <value>The tag-addressed property collection.</value>
    public MapiPropertyCollection Properties { get; }

    /// <summary>
    /// Gets how the recipient participates in the message.
    /// </summary>
    /// <value>The <c>PidTagRecipientType</c> value when present and defined; otherwise <see langword="null" />.</value>
    public OutlookRecipientType? RecipientType =>
        Properties.GetInt32(MapiPropertyIds.RecipientType) is int value
            && value is >= (int)OutlookRecipientType.Originator and <= (int)OutlookRecipientType.Bcc
            ? (OutlookRecipientType)value
            : null;

    /// <summary>
    /// Gets the recipient display name.
    /// </summary>
    /// <value>The <c>PidTagDisplayName</c> value, or <see langword="null" /> when absent.</value>
    public string? DisplayName =>
        Properties.GetString(MapiPropertyIds.DisplayName);

    /// <summary>
    /// Gets the recipient email address.
    /// </summary>
    /// <value>The <c>PidTagEmailAddress</c> value, or <see langword="null" /> when absent.</value>
    public string? EmailAddress =>
        Properties.GetString(MapiPropertyIds.EmailAddress);

    /// <summary>
    /// Gets the recipient address type (for example, <c>SMTP</c> or <c>EX</c>).
    /// </summary>
    /// <value>The <c>PidTagAddressType</c> value, or <see langword="null" /> when absent.</value>
    public string? AddressType =>
        Properties.GetString(MapiPropertyIds.AddressType);

    /// <summary>
    /// Returns a textual form of the recipient for diagnostics.
    /// </summary>
    /// <returns>The recipient type and display name or address.</returns>
    public override string ToString() =>
        $"{RecipientType?.ToString() ?? "Unknown"}: {DisplayName ?? EmailAddress ?? "(unnamed)"}";
}
