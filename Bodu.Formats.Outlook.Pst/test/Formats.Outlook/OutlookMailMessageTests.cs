// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMailMessage" />, the message view. This root holds the synthetic-store
/// helpers the member partials share; the synthetic fixture is the content oracle, the reference corpus the
/// structural one.
/// </summary>
[TestClass]
public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Opens a synthetic mail store built by <see cref="PstMessagingFixtureBuilder" />.
    /// </summary>
    /// <param name="configure">An optional knob configuration applied before the fixture is assembled.</param>
    /// <param name="level">The validation level to open under.</param>
    /// <returns>The open session; the caller disposes it.</returns>
    internal static OutlookMailStore OpenSynthetic(
        Action<PstMessagingFixtureBuilder>? configure = null,
        PstValidationLevel level = PstValidationLevel.Compatible)
    {
        var builder = new PstMessagingFixtureBuilder();
        configure?.Invoke(builder);

        return OutlookMailStore.Open(
            builder.BuildStream(),
            new OutlookMailStoreReaderOptions { ValidationLevel = level });
    }

    /// <summary>
    /// Retrieves the synthetic store's full message — the one carrying recipient and attachment tables.
    /// </summary>
    /// <param name="store">The open synthetic session.</param>
    /// <returns>The message view.</returns>
    internal static OutlookMailMessage GetFullMessage(OutlookMailStore store) =>
        store.RootFolder.EnumerateSubfolders().Single()
            .EnumerateMessages()
            .Single(static m => m.Subject == PstMessagingFixtureBuilder.NormalizedSubject);

    /// <summary>
    /// Retrieves the synthetic store's plain message — the one with no subnode tree at all.
    /// </summary>
    /// <param name="store">The open synthetic session.</param>
    /// <returns>The message view.</returns>
    internal static OutlookMailMessage GetPlainMessage(OutlookMailStore store) =>
        store.RootFolder.EnumerateSubfolders().Single()
            .EnumerateMessages()
            .Single(static m => m.Subject == PstMessagingFixtureBuilder.PlainSubject);
}
