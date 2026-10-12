// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookOmittedValueReason.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Specifies why the mail-store reader omitted a property value from an object it decoded.
/// </summary>
public enum OutlookOmittedValueReason
{
    /// <summary>
    /// The value's reference into the container does not resolve: it names a heap item, or a subnode, that the
    /// container does not hold, or a fixed-width value is shorter than its type. Such a value is omitted only when
    /// <see cref="OutlookMailStoreReaderOptions.OmitUnresolvableValues" /> is set; otherwise it raises
    /// <see cref="PstFileFormatException" />.
    /// </summary>
    UnresolvableReference = 0,

    /// <summary>
    /// The value resolves but cannot be decoded as its MAPI property type, for example a multi-valued value too short
    /// for its count. Such a value is omitted under <see cref="PstValidationLevel.Compatible" /> and
    /// <see cref="PstValidationLevel.Minimal" />; under <see cref="PstValidationLevel.Strict" /> it raises
    /// <see cref="OutlookPstFormatException" />.
    /// </summary>
    UndecodableValue = 1,
}
