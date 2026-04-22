// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.TableType.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography;

public partial class PearsonTests
{
    /// <summary>
    /// Provides test data for each predefined <see cref="Pearson.PearsonTableType" /> (excluding UserDefined).
    /// </summary>
    public static IEnumerable<object[]> TableTypeTestData =>
        Enum.GetValues<Pearson.PearsonTableType>()
            .Where(t => t != Pearson.PearsonTableType.UserDefined)
            .Select(t => new object[] { t });

    /// <summary>
    /// Verifies that setting <see cref="Pearson.TableType" /> to a valid predefined type allows computing a hash without error, and
    /// that the hash output is the correct length for the current <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    /// <param name="type">The table type to test.</param>
    [TestMethod]
    [DynamicData(nameof(TableTypeTestData))]
    public void TableType_Set_WhenValid_ShouldProduceExpectedHash(Pearson.PearsonTableType type)
    {
        using var algorithm = new Pearson
        {
            TableType = type,
            HashSize = 64
        };

        byte[] input = Encoding.ASCII.GetBytes("Test input");

        byte[] result = algorithm.ComputeHash(input);

        Assert.IsNotNull(result, $"Result should not be null for table type {type}.");
        Assert.AreEqual(algorithm.HashSize / 8, result.Length, $"Hash length should match expected size for type {type}.");
        Assert.IsTrue(result.Any(b => b != 0), $"Hash result should not be all zeros for type {type}.");
    }

    /// <summary>
    /// Verifies that <see cref="Pearson.TableType" />, Get, when Default, returns the expected value.
    /// </summary>
    [TestMethod]
    public void TableType_Get_WhenDefault_ShouldReturnPearson()
    {
        using var algorithm = new Pearson();

        var type = algorithm.TableType;

        Assert.AreEqual(Pearson.PearsonTableType.Pearson, type, "Default TableType should be Pearson.");
    }

    /// <summary>
    /// Verifies that <see cref="Pearson.TableType" />, Set, when NotStarted, returns the expected value.
    /// </summary>
    [TestMethod]
    public void TableType_Set_WhenNotStarted_ShouldLoadExpectedPresetTable()
    {
        using var algorithm = new Pearson();
        var lazy = typeof(Pearson)
            .GetField("AESSBoxTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as Lazy<byte[]>;
        var expected = lazy?.Value ?? Array.Empty<byte>();
        algorithm.TableType = Pearson.PearsonTableType.AESSBox;
        var table = algorithm.Table;

        Assert.AreEqual(256, table.Length, "Table length should be 256.");
        CollectionAssert.AreEqual(expected, table, "Table contents should match AESSBox preset.");
    }

    /// <summary>
    /// Verifies that <see cref="Pearson.TableType" />, Set, ToUserDefined, throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void TableType_Set_ToUserDefined_ShouldRequireManualTableAssignment()
    {
        using var algorithm = new Pearson();

        algorithm.TableType = Pearson.PearsonTableType.UserDefined;

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.ComputeHash(new byte[] { 1, 2, 3 });
        }, "Using UserDefined table type without a valid table should throw.");
    }

    /// <summary>
    /// Verifies that <see cref="Pearson.TableType" />, Set, when HashingStarted, throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void TableType_Set_WhenHashingStarted_ShouldThrowExactly()
    {
        using var algorithm = new Pearson();
        algorithm.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.TableType = Pearson.PearsonTableType.SHA256Constants;
        });
    }

    /// <summary>
    /// Verifies that <see cref="Pearson.TableType" />, Set, when Disposed, throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void TableType_Set_WhenDisposed_ShouldThrowExactly()
    {
        var algorithm = new Pearson();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.TableType = Pearson.PearsonTableType.CRC32HighByte;
        });
    }

    /// <summary>
    /// Verifies that <see cref="Pearson.TableType" />, Get, when Disposed, throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void TableType_Get_WhenDisposed_ShouldThrowExactly()
    {
        var algorithm = new Pearson();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            var _ = algorithm.TableType;
        });
    }

    /// <summary>
    /// Verifies that hashing with <see cref="Pearson.PearsonTableType.UserDefined" /> selected
    /// but no table assigned throws an exception whose message does not contain any
    /// refactor artefacts ("this." prefixes or stray " t " tokens).
    /// </summary>
    [TestMethod]
    public void TableType_WhenSetToUserDefinedWithoutTable_ShouldThrowWithCleanMessage()
    {
        using var algorithm = new Pearson
        {
            TableType = Pearson.PearsonTableType.UserDefined
        };

        var ex = Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.ComputeHash(new byte[] { 1, 2, 3 });
        });

        Assert.IsFalse(
            ex.Message.Contains(" t "),
            $"Exception message should not contain the bare token ' t '. Actual: {ex.Message}");
        Assert.IsFalse(
            ex.Message.Contains("this."),
            $"Exception message should not contain a 'this.' artefact. Actual: {ex.Message}");
    }

}
