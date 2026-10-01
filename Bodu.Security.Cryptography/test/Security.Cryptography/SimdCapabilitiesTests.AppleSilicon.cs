// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilitiesTests.AppleSilicon.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

public sealed partial class SimdCapabilitiesTests
{
    /// <summary>
    /// Verifies that an ARM64 process on one of Apple's operating systems is reported as running on Apple's cores.
    /// </summary>
    [TestMethod]
    public void AppleSilicon_WhenProcessIsArm64OnAnApplePlatform_ShouldBeTrue()
    {
        if (!IsArm64OnAnApplePlatform())
            Assert.Inconclusive("The process is not running on ARM64 under one of Apple's operating systems.");

        Assert.IsTrue(SimdCapabilities.AppleSilicon);
    }

    /// <summary>
    /// Verifies that a process on any other operating system or architecture, Linux on ARM64 included, is not reported
    /// as running on Apple's cores.
    /// </summary>
    [TestMethod]
    public void AppleSilicon_WhenProcessIsNotArm64OnAnApplePlatform_ShouldBeFalse()
    {
        if (IsArm64OnAnApplePlatform())
            Assert.Inconclusive("The process is running on ARM64 under one of Apple's operating systems.");

        Assert.IsFalse(SimdCapabilities.AppleSilicon);
    }

    /// <summary>
    /// Determines whether the process runs on ARM64 under macOS, iOS, tvOS, or Mac Catalyst.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the process runs on ARM64 under one of Apple's operating systems; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool IsArm64OnAnApplePlatform() =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            && (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst());
}
