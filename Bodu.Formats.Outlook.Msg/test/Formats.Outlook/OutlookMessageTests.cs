// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMessage" />, the <c>.msg</c> reading session.
/// </summary>
[TestClass]
public partial class OutlookMessageTests
{
    /// <summary>
    /// Builds the minimal well-formed message container shared by the member tests.
    /// </summary>
    /// <returns>A seekable stream positioned at the container start.</returns>
    internal static MemoryStream CreateMinimalContainer() =>
        MsgFixtureBuilder.CreateMinimal().Build();
}
