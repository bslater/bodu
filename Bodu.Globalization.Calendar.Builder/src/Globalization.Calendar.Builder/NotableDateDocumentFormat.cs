// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Identifies the on-disk serialization form of a notable-date document produced by a
/// <see cref="NotableDateDocumentBuilder" />.
/// </summary>
public enum NotableDateDocumentFormat
{
    /// <summary>
    /// The full-fidelity XML form in the <c>urn:bodu:globalization:calendar</c> namespace, capable of expressing every
    /// feature of the notable-date schema.
    /// </summary>
    Xml = 0,

    /// <summary>
    /// The narrower JSON subset form, which omits imports, restricts calendars to Gregorian, and supports only the
    /// reduced trigger, action, and override surface defined by the companion JSON schema.
    /// </summary>
    Json = 1,

    /// <summary>
    /// The sealed binary rule-pack form (<c>.bcal</c>): the document is built through the canonical loader and the
    /// validated resource is encoded by <see cref="Bodu.Globalization.Calendar.NotableDateBinaryResource" />. A pack is
    /// compiled output — it loads through
    /// <see cref="Bodu.Globalization.Calendar.NotableDateResourceLoader.LoadBinary(System.IO.Stream)" /> and cannot be
    /// re-opened for editing with <see cref="NotableDateDocumentBuilder.Load(string)" />.
    /// </summary>
    Binary = 2,
}
