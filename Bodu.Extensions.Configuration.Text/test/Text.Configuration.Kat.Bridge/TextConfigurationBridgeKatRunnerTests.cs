// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBridgeKatRunnerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Bodu.Test.IO;
using Bodu.Test.Kat;
using Bodu.Text.Configuration.Test.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Text.Configuration.Kat.Bridge;

/// <summary>
/// Drives the <see cref="Bodu.Text.Configuration.Test.Infrastructure.ConfigurationKatKind.ConfigurationBridge" />
/// KAT subset, exercising the Microsoft.Extensions.Configuration bridge end to end.
/// </summary>
[TestClass]
public partial class TextConfigurationBridgeKatRunnerTests
{
    /// <summary>
    /// Verifies that a valid bridge KAT builds the configuration and exposes the expected values.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.BridgeDataPass),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Bridge_WhenValid_ShouldMatchExpectedConfiguration(ConfigurationKat kat)
    {
        ExecuteBridgePass(kat);
    }

    /// <summary>
    /// Verifies that an invalid bridge KAT throws the expected exception when the configuration is built.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.BridgeDataFail),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Bridge_WhenInvalid_ShouldThrowExpectedException(ConfigurationKat kat)
    {
        ExecuteBridgeFail(kat);
    }

    private static void ExecuteBridgePass(ConfigurationKat kat)
    {
        bool optionalMissing = kat.Options is "OptionalTrueMissingFile";

        if (kat.Source is null)
        {
            // Optional missing file scenario — no file is created, but the builder is configured to expect one.
            _ = new ConfigurationBuilder()
                .AddTextConfigurationFile(source =>
                {
                    source.FileProvider = new PhysicalFileProvider(Path.GetTempPath());
                    source.Path = "this-file-does-not-exist.boduconfig";
                    source.Optional = optionalMissing;
                    source.TargetPath = kat.TargetPath;
                })
                .Build();

            Assert.IsEmpty(kat.ExpectedValues, $"{kat.Id}: optional missing file expected no values.");
            return;
        }

        using TempFileScope scope = new(kat.Source, "boduconfig");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationFile(source =>
            {
                source.FileProvider = new PhysicalFileProvider(scope.Directory);
                source.Path = Path.GetFileName(scope.Path);
                source.TargetPath = kat.TargetPath;
            })
            .Build();

        foreach (ExpectedValue ev in kat.ExpectedValues)
            Assert.AreEqual(ev.Value, configuration[ev.Key], $"{kat.Id}: bridge value for '{ev.Key}'.");
    }

    private static void ExecuteBridgeFail(ConfigurationKat kat)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id}: fail KAT requires an ExpectedException.");

        try
        {
            _ = new ConfigurationBuilder()
                .AddTextConfigurationFile(source =>
                {
                    source.FileProvider = new PhysicalFileProvider(Path.GetTempPath());
                    source.Path = "this-file-does-not-exist.boduconfig";
                    source.Optional = false;
                    source.TargetPath = kat.TargetPath;
                })
                .Build();

            Assert.Fail($"{kat.Id}: expected exception '{kat.ExpectedException}' was not thrown.");
        }
        catch (Exception ex)
        {
            Assert.AreEqual(
                kat.ExpectedException,
                ex.GetType().Name,
                $"{kat.Id}: expected '{kat.ExpectedException}' but got '{ex.GetType().Name}': {ex.Message}");
        }
    }
}
