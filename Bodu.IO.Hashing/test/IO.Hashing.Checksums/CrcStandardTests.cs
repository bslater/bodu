// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for <see cref="CrcStandard" /> covering construction guards, equality semantics, and the
/// <see cref="System.Runtime.Serialization.ISerializable" /> round-trip contract.
/// </summary>
[TestClass]
public partial class CrcStandardTests
{

    private static CrcStandard CreateReference(
        string name = "Test",
        int size = 32,
        ulong polynomial = 0x1021UL,
        ulong initialValue = 0xFFFFUL,
        bool reflectIn = true,
        bool reflectOut = true,
        ulong xOrOut = 0xFFFFUL)
        => new(name, size, polynomial, initialValue, reflectIn, reflectOut, xOrOut);

}
