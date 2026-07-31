// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyStreamReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the behavior of <see cref="MsgPropertyStreamReader" />, the property-stream parser.
/// </summary>
[TestClass]
public partial class MsgPropertyStreamReaderTests
{
    /// <summary>
    /// Gets malformed property-stream payloads that must be rejected for the root header shape.
    /// </summary>
    /// <value>Rows of payload bytes expected to throw <see cref="OutlookMsgFormatException" />.</value>
    public static IEnumerable<object[]> MalformedRootPayloads =>
        new InvalidKat<byte[]>[]
        {
            new("Empty", Array.Empty<byte>(), typeof(OutlookMsgFormatException)),
            new("ShorterThanHeader", new byte[31], typeof(OutlookMsgFormatException)),
            new("TrailingPartialRecord", new byte[32 + 15], typeof(OutlookMsgFormatException)),
            new("RecordBoundaryOffByOne", new byte[32 + 16 + 1], typeof(OutlookMsgFormatException)),
        }.Select(kat => new object[] { kat });
}
