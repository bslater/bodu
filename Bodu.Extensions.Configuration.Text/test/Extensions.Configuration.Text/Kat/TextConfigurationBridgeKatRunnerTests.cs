// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBridgeKatRunnerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Bodu.Text.Configuration.Test.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Text.Configuration.Kat.Bridge;

/// <summary>
/// Drives the <see cref="Bodu.Text.Configuration.Test.Infrastructure.ConfigurationKatKind.ConfigurationBridge" />
/// KAT subset, exercising the Microsoft.Extensions.Configuration bridge end to end.
/// </summary>
[TestClass]
public class TextConfigurationBridgeKatRunnerTests
{
    /// <summary>
    /// Drives every bridge KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.BridgeData),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Bridge_Kat(ConfigurationKat kat)
    {
        if (kat.Outcome is ConfigurationKatOutcome.Fail)
        {
            ExecuteBridgeFail(kat);
            return;
        }

        ExecuteBridgePass(kat);
    }

    private static void ExecuteBridgePass(ConfigurationKat kat)
    {
        var optionalMissing = kat.Options is "OptionalTrueMissingFile";

        if (kat.Source is null)
        {
            // Optional missing file scenario — no file is created, but the builder is configured to expect one.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddBoduConfigurationFile(source =>
                {
                    source.FileProvider = new PhysicalFileProvider(Path.GetTempPath());
                    source.Path = "this-file-does-not-exist.boduconfig";
                    source.Optional = optionalMissing;
                    source.TargetPath = kat.TargetPath;
                })
                .Build();

            Assert.AreEqual(0, kat.ExpectedValues.Count, $"{kat.Id}: optional missing file expected no values.");
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".boduconfig");
        try
        {
            File.WriteAllText(path, kat.Source);

            IConfiguration configuration = new ConfigurationBuilder()
                .AddBoduConfigurationFile(source =>
                {
                    source.FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(path)!);
                    source.Path = Path.GetFileName(path);
                    source.TargetPath = kat.TargetPath;
                })
                .Build();

            foreach (ExpectedValue ev in kat.ExpectedValues)
                Assert.AreEqual(ev.Value, configuration[ev.Key], $"{kat.Id}: bridge value for '{ev.Key}'.");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void ExecuteBridgeFail(ConfigurationKat kat)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id}: fail KAT requires an ExpectedException.");

        try
        {
            _ = new ConfigurationBuilder()
                .AddBoduConfigurationFile(source =>
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

    /// <summary>
    /// Supplies the per-row display name used by the MSTest runner for KAT-driven tests.
    /// </summary>
    /// <param name="methodInfo">The driver method.</param>
    /// <param name="data">The row data; the single element is a <see cref="ConfigurationKat" />.</param>
    /// <returns>A short identifier derived from the KAT's stable ID and title.</returns>
    public static string GetKatDisplayName(System.Reflection.MethodInfo methodInfo, object[] data)
    {
        var kat = (ConfigurationKat)data[0];
        return $"{kat.Id} - {kat.Title}";
    }
}
