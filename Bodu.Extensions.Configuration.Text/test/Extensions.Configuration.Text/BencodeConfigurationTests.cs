// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConfigurationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Behavioural tests for the read-only Bencode configuration source — flattening into the colon-delimited key
/// model, file and stream loading, and the read-only contract.
/// </summary>
[TestClass]
public sealed partial class BencodeConfigurationTests
{
    private const string Sample =
        "d" +
        "7:loggingd6:levelsl4:info4:warnee" +
        "6:serverd7:enabledi1e4:host9:localhost4:porti8080ee" +
        "e";

    private static IConfigurationRoot BuildFromStream(string bencode)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(bencode));
        return new ConfigurationBuilder().AddBencodeStream(stream).Build();
    }
}
